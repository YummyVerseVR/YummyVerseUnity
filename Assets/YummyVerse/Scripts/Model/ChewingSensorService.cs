using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;
using YummyVerse.Scripts.Model.Struct.SO;
using Zenject;

namespace YummyVerse.Scripts.Model
{
    /// <summary>
    /// 咀嚼計との常駐接続 (プロトコル仕様書 YV-SERIAL-001)。
    ///
    /// スレッドの分担:
    ///   受信スレッド … COMポート探索、HELLO/READY ハンドシェイク、送受信、切断検知。
    ///   メインスレッド … 受信キューの消費、キャリブレーションの保留管理、R3 への発行。
    ///
    /// メインスレッドを ReadLine でブロックしないこと、解析済みイベントをスレッドセーフな
    /// キュー経由で渡すことは仕様書 §15.1 の要件である。
    /// </summary>
    public sealed class ChewingSensorService : IChewingSensorService, IInitializable, ITickable, IDisposable
    {
        private readonly ISerialPortProvider _portProvider;
        private readonly ChewingSensorConfig _config;

        private readonly ConcurrentQueue<ChewingSensorMessage> _inbound = new();
        private readonly ConcurrentQueue<byte[]> _outbound = new();

        private readonly ReactiveProperty<ChewingSensorConnectionState> _connectionState = new(
            ChewingSensorConnectionState.Disconnected);
        private readonly Subject<MouthState> _onMouthEvent = new();

        private readonly ChewingRequestIdSequence _requestIds = new();

        /// <summary>受信スレッドが書き、メインスレッドが読む接続状態。</summary>
        private int _workerConnectionState = (int)ChewingSensorConnectionState.Disconnected;

        /// <summary>
        /// 接続が張り直されるたびに増える世代番号。
        /// 古い接続へ出した要求へ新しい接続の応答を紐づけないための照合子 (仕様書 §10)。
        /// </summary>
        private int _connectionEpoch;

        private Thread _worker;
        private CancellationTokenSource _workerCts;

        private CalibrationRequest _pending;

        public ReadOnlyReactiveProperty<ChewingSensorConnectionState> ConnectionState => _connectionState;
        public Observable<MouthState> OnMouthEvent => _onMouthEvent;

        public ChewingSensorService(ISerialPortProvider portProvider, ChewingSensorConfig config)
        {
            _portProvider = portProvider;
            _config = config;
        }

        public void Initialize()
        {
            _workerCts = new CancellationTokenSource();
            var token = _workerCts.Token;

            _worker = new Thread(() => WorkerLoop(token))
            {
                Name = "ChewingSensorSerial",
                IsBackground = true
            };
            _worker.Start();
        }

        // ------------------------------------------------------------------
        // メインスレッド
        // ------------------------------------------------------------------

        public void Tick()
        {
            var workerState = (ChewingSensorConnectionState)Volatile.Read(ref _workerConnectionState);
            if (_connectionState.Value != workerState) _connectionState.Value = workerState;

            // 溜め込むと咀嚼音が遅れて追いかけてくるので、来ている分はこのフレームで出し切る。
            while (_inbound.TryDequeue(out var message)) Dispatch(message);

            UpdatePendingCalibration();
        }

        public async UniTask<ChewingCalibrationResult> CalibrateAsync(
            IChewingCalibrationPrompt prompt, CancellationToken ct)
        {
            if (_connectionState.Value != ChewingSensorConnectionState.Connected)
            {
                Debug.LogWarning("[ChewingSensor] 咀嚼計へ接続していないため、キャリブレーションを要求できません");
                return ChewingCalibrationResult.NotConnected();
            }

            if (_pending != null)
            {
                // 仕様書 §9.1: Unity が同時に保留できる要求は1件だけ。
                Debug.LogWarning("[ChewingSensor] 既にキャリブレーションを要求中のため、新しい要求は受け付けません");
                return ChewingCalibrationResult.Failed("BUSY");
            }

            var request = new CalibrationRequest(_requestIds.Next(), Volatile.Read(ref _connectionEpoch));
            _pending = request;

            try
            {
                return await RunCalibrationAsync(request, prompt, ct);
            }
            catch (OperationCanceledException)
            {
                // 咀嚼計はフェーズ指示を無期限に待つ。黙って待つのをやめるとデバイス側に
                // 状態が残り、次の来場者の要求が BUSY で弾かれる (仕様書 §9.5, §9.7)。
                SendCalibrationAbort(request);
                throw;
            }
            finally
            {
                if (ReferenceEquals(_pending, request)) _pending = null;
            }
        }

        /// <summary>
        /// 受理 → ノイズ測定 → 咀嚼測定 の順に進める (仕様書 §9.1, §9.3)。
        /// 各フェーズ要求は案内が終わってから送るため、送信の判断はここに集約する。
        /// </summary>
        private async UniTask<ChewingCalibrationResult> RunCalibrationAsync(
            CalibrationRequest request, IChewingCalibrationPrompt prompt, CancellationToken ct)
        {
            // 受理待ち。CAL_ACCEPTED の時点ではまだ測定は始まっていない。
            request.BeginStage(CalibrationStage.AwaitingAccept);
            SendCalibrationStart(request);
            await request.StageCompleted.AttachExternalCancellation(ct);
            if (request.Terminal.HasValue) return request.Terminal.Value;

            // ノイズ測定。CAL_NOISE_DONE を受けたら次の案内へ進む。
            if (!await RunPhaseAsync(request, prompt, ChewingCalibrationPhase.Noise, ct))
            {
                return request.Terminal ?? ChewingCalibrationResult.TimedOut();
            }

            // 咀嚼測定。CAL_CHEW_DONE の後、閾値を保存した CAL_DONE が終端になる (仕様書 §9.1)。
            await RunPhaseAsync(request, prompt, ChewingCalibrationPhase.Chew, ct);
            return request.Terminal ?? ChewingCalibrationResult.TimedOut();
        }

        /// <summary>
        /// 1フェーズぶんの案内と測定。決着せずに次へ進める場合だけ true を返す。
        /// </summary>
        private async UniTask<bool> RunPhaseAsync(
            CalibrationRequest request,
            IChewingCalibrationPrompt prompt,
            ChewingCalibrationPhase phase,
            CancellationToken ct)
        {
            // 案内とカウントダウンの間は何も送らない。咀嚼計は待ってくれる (仕様書 §9.7)。
            request.BeginStage(CalibrationStage.Prompting);
            await prompt.PrepareAsync(phase, ct);

            // 案内中に切断された場合は送らずに終える。
            if (request.Terminal.HasValue) return false;

            // 送信の直後から測定が始まるので、カウントが0になったこの時点で送る (仕様書 §9.2)。
            request.BeginStage(
                phase == ChewingCalibrationPhase.Noise
                    ? CalibrationStage.MeasuringNoise
                    : CalibrationStage.MeasuringChew);
            Send(ChewingSensorProtocol.BuildCalibrationPhase(phase, request.RequestId));

            await request.StageCompleted.AttachExternalCancellation(ct);
            return !request.Terminal.HasValue;
        }

        private void Dispatch(ChewingSensorMessage message)
        {
            switch (message.Kind)
            {
                case ChewingSensorMessageKind.Mouth:
                    _onMouthEvent.OnNext(message.MouthState);
                    return;

                case ChewingSensorMessageKind.Ready:
                    // ハンドシェイクは受信スレッドで完結している。自発的な READY はここでは何もしない。
                    return;

                case ChewingSensorMessageKind.CalibrationAccepted:
                case ChewingSensorMessageKind.CalibrationNoiseDone:
                case ChewingSensorMessageKind.CalibrationChewDone:
                case ChewingSensorMessageKind.CalibrationDone:
                case ChewingSensorMessageKind.CalibrationFailed:
                    // 送信だけログに出ていると、応答が来ていないのか、来ているが状態と噛み合って
                    // いないのかを切り分けられない。キャリブレーション系は必ず受信も残す。
                    // (MOUTH は咀嚼のたびに流れるので、ここでは出さない。)
                    Debug.Log($"[ChewingSensor] 受信: {message}");
                    DispatchCalibration(message);
                    return;
            }
        }

        private void DispatchCalibration(ChewingSensorMessage message)
        {
            var pending = _pending;
            if (pending == null || pending.RequestId != message.RequestId)
            {
                // 取り消し済み・再接続前の要求への応答。requestId は一致判定だけで扱う (仕様書 §10)。
                Debug.Log($"[ChewingSensor] 保留中の要求と対応しない応答を破棄します: {message}");
                return;
            }

            switch (message.Kind)
            {
                case ChewingSensorMessageKind.CalibrationAccepted:
                    // CAL_START 再送への重複応答で案内をやり直さない (仕様書 §9.6)。
                    if (pending.Stage == CalibrationStage.AwaitingAccept) pending.CompleteStage();
                    return;

                case ChewingSensorMessageKind.CalibrationNoiseDone:
                    if (pending.Stage == CalibrationStage.MeasuringNoise) pending.CompleteStage();
                    return;

                case ChewingSensorMessageKind.CalibrationChewDone:
                    // 閾値の確定・保存はこの後。終端は CAL_DONE なので、待ち直すだけにする。
                    if (pending.Stage == CalibrationStage.MeasuringChew) pending.RestartStageDeadline();
                    return;

                case ChewingSensorMessageKind.CalibrationDone:
                    if (pending.Stage != CalibrationStage.MeasuringChew)
                    {
                        // 咀嚼測定まで進む前に終端が来た。フェーズ応答 (CAL_NOISE_DONE /
                        // CAL_CHEW_DONE) を返さない v1.0 相当のファームウェアだとここに来る。
                        // 咀嚼計は測定を終えているので案内だけ足しても意味がなく、そのまま完了させる。
                        Debug.LogWarning(
                            $"[ChewingSensor] {Describe(pending.Stage)}の段階で CAL_DONE を受信しました。" +
                            "咀嚼計がフェーズ分割 (CAL_NOISE_DONE / CAL_CHEW_DONE) に対応していない可能性が" +
                            "あります。この場合、咀嚼測定の案内は表示されません。");
                    }

                    Complete(pending, ChewingCalibrationResult.Succeeded());
                    return;

                case ChewingSensorMessageKind.CalibrationFailed:
                    // どの段で断られたのかが分からないと、装着不良なのか順序違反なのかを切り分けられない。
                    Debug.LogWarning(
                        $"[ChewingSensor] {Describe(pending.Stage)}で咀嚼計がキャリブレーションを拒否しました: " +
                        $"{message.FailureReason}");
                    Complete(pending, ChewingCalibrationResult.Failed(message.FailureReason));
                    return;
            }
        }

        /// <summary>
        /// 再送とタイムアウトの面倒を見る (仕様書 §13)。
        /// 再送では新しい requestId を発行しない。発行すると咀嚼計が受理済みだった場合に二重要求になる。
        /// </summary>
        private void UpdatePendingCalibration()
        {
            var pending = _pending;
            if (pending == null) return;

            if (pending.Epoch != Volatile.Read(ref _connectionEpoch))
            {
                // 仕様書 §10: 新しい接続を確立したら、古い接続の保留要求は失敗として終わらせる。
                Debug.LogWarning("[ChewingSensor] 要求中に接続が切れたため、キャリブレーションを打ち切ります");
                Complete(pending, ChewingCalibrationResult.NotConnected());
                return;
            }

            var now = Time.realtimeSinceStartup;

            switch (pending.Stage)
            {
                case CalibrationStage.AwaitingAccept:
                    if (now - pending.LastSentAt < _config.CalibrationAcceptedTimeoutSeconds) return;

                    if (pending.Attempts >= Mathf.Max(1, _config.CalibrationStartAttempts))
                    {
                        Debug.LogWarning("[ChewingSensor] CAL_ACCEPTED が返らないため、キャリブレーションを断念します");
                        Complete(pending, ChewingCalibrationResult.TimedOut());
                        return;
                    }

                    SendCalibrationStart(pending);
                    return;

                case CalibrationStage.Prompting:
                    // 案内とカウントダウンの間。咀嚼計は待つと決まっているので急かさない (仕様書 §9.7)。
                    return;

                case CalibrationStage.MeasuringNoise:
                case CalibrationStage.MeasuringChew:
                    var limit = pending.Stage == CalibrationStage.MeasuringNoise
                        ? _config.CalibrationNoiseTimeoutSeconds
                        : _config.CalibrationChewTimeoutSeconds;
                    if (now - pending.StageStartedAt < limit) return;

                    Debug.LogWarning(
                        $"[ChewingSensor] {Describe(pending.Stage)}の完了応答が {limit} 秒返らないため、" +
                        "キャリブレーションを断念します");
                    SendCalibrationAbort(pending);
                    Complete(pending, ChewingCalibrationResult.TimedOut());
                    return;
            }
        }

        private static string Describe(CalibrationStage stage) => stage switch
        {
            CalibrationStage.AwaitingAccept => "受理待ち",
            CalibrationStage.Prompting => "案内表示中",
            CalibrationStage.MeasuringNoise => "ノイズ測定 (CAL_NOISE)",
            CalibrationStage.MeasuringChew => "咀嚼測定 (CAL_CHEW)",
            _ => stage.ToString()
        };

        private void SendCalibrationStart(CalibrationRequest request)
        {
            // 再送でも新しい requestId を発行しない。受理済みだった場合に二重要求になる (仕様書 §13)。
            request.MarkSent(Time.realtimeSinceStartup);
            Send(ChewingSensorProtocol.BuildCalibrationStart(request.RequestId));
        }

        /// <summary>放棄する要求のフェーズ状態を咀嚼計側にも捨てさせる (仕様書 §9.5)。</summary>
        private void SendCalibrationAbort(CalibrationRequest request)
        {
            if (request.Terminal.HasValue) return;

            // 接続が変わっていれば、そのポートへ送っても意味がない。
            if (request.Epoch != Volatile.Read(ref _connectionEpoch)) return;

            Send(ChewingSensorProtocol.BuildCalibrationAbort(request.RequestId));
        }

        private void Complete(CalibrationRequest request, ChewingCalibrationResult result)
        {
            if (ReferenceEquals(_pending, request)) _pending = null;
            request.Complete(result);
        }

        private void Send(string message)
        {
            Debug.Log($"[ChewingSensor] 送信: {message}");
            var payload = Encoding.UTF8.GetBytes(message + "\n");
            _outbound.Enqueue(payload);
        }

        // ------------------------------------------------------------------
        // 受信スレッド
        // ------------------------------------------------------------------

        private void WorkerLoop(CancellationToken ct)
        {
            var assembler = new SerialLineAssembler();
            var lines = new List<string>();

            while (!ct.IsCancellationRequested)
            {
                ISerialPortConnection connection = null;
                try
                {
                    connection = Discover(assembler, lines, ct);
                    if (connection == null)
                    {
                        SetWorkerState(ChewingSensorConnectionState.Disconnected);
                        WaitFor(_config.RediscoverIntervalSeconds, ct);
                        continue;
                    }

                    Debug.Log($"[ChewingSensor] 咀嚼計を {connection.PortName} で検出しました");
                    BeginConnection();
                    Exchange(connection, assembler, lines, ct);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception e)
                {
                    // 未知・不正な行や一時的なI/O失敗で受信ループ全体を止めない (仕様書 §15.1)。
                    Debug.LogWarning($"[ChewingSensor] 通信を中断しました: {e.Message}");
                }
                finally
                {
                    connection?.Dispose();
                    EndConnection(assembler);
                }
            }
        }

        /// <summary>
        /// 候補ポートへ順に HELLO を送り、正しい READY を返したポートを採用する (仕様書 §6.1)。
        /// ハンドシェイク成立前に HELLO 以外を送らないことで、無関係な機器への影響を抑える。
        /// </summary>
        private ISerialPortConnection Discover(
            SerialLineAssembler assembler, List<string> lines, CancellationToken ct)
        {
            SetWorkerState(ChewingSensorConnectionState.Discovering);

            var settings = _config.ToSerialPortSettings();
            var hello = Encoding.UTF8.GetBytes(ChewingSensorProtocol.HelloMessage + "\n");
            var buffer = new byte[256];

            foreach (var portName in OrderCandidates(_portProvider.ListPortNames()))
            {
                ct.ThrowIfCancellationRequested();

                ISerialPortConnection connection = null;
                try
                {
                    connection = _portProvider.Open(portName, settings);
                    connection.DiscardBuffers();
                    assembler.Reset();
                    lines.Clear();

                    var deadline = DateTime.UtcNow.AddSeconds(_config.PortProbeTimeoutSeconds);
                    var nextHelloAt = DateTime.MinValue;

                    while (DateTime.UtcNow < deadline)
                    {
                        ct.ThrowIfCancellationRequested();

                        if (DateTime.UtcNow >= nextHelloAt)
                        {
                            // ポートオープン時のデバイスリセットで初回が消えることがあるため再送する。
                            connection.Write(hello, 0, hello.Length);
                            nextHelloAt = DateTime.UtcNow.AddSeconds(_config.HelloRetryIntervalSeconds);
                        }

                        var read = connection.Read(buffer, 0, buffer.Length);
                        if (read <= 0) continue;

                        lines.Clear();
                        assembler.Append(buffer, 0, read, lines);
                        foreach (var line in lines)
                        {
                            if (!ChewingSensorProtocol.TryParse(line, out var message)) continue;
                            if (message.Kind != ChewingSensorMessageKind.Ready) continue;

                            var adopted = connection;
                            connection = null;
                            return adopted;
                        }
                    }
                }
                catch (Exception e) when (!(e is OperationCanceledException))
                {
                    // 開けない・応答しないポートは珍しくない。次の候補へ進むだけにする。
                    Debug.Log($"[ChewingSensor] {portName} は候補から外します: {e.Message}");
                }
                finally
                {
                    connection?.Dispose();
                }
            }

            return null;
        }

        /// <summary>
        /// 送信キューの掃き出しと受信を1本のスレッドで交互に行う。
        /// Read は設定した読み取りタイムアウトで必ず戻るので、送信が待たされ続けることはない。
        /// </summary>
        private void Exchange(
            ISerialPortConnection connection, SerialLineAssembler assembler, List<string> lines, CancellationToken ct)
        {
            var buffer = new byte[256];

            while (!ct.IsCancellationRequested)
            {
                while (_outbound.TryDequeue(out var payload))
                {
                    connection.Write(payload, 0, payload.Length);
                }

                var read = connection.Read(buffer, 0, buffer.Length);
                if (read <= 0) continue;

                lines.Clear();
                assembler.Append(buffer, 0, read, lines);
                foreach (var line in lines)
                {
                    if (ChewingSensorProtocol.TryParse(line, out var message))
                    {
                        _inbound.Enqueue(message);
                        continue;
                    }

                    Debug.LogWarning($"[ChewingSensor] 解釈できない受信行を破棄しました: {line}");
                }
            }

            ct.ThrowIfCancellationRequested();
        }

        /// <summary>
        /// 見つかりやすい順に並べ替えるだけで、候補から外すことはしない (仕様書 §6.2)。
        /// </summary>
        private IEnumerable<string> OrderCandidates(IReadOnlyList<string> portNames)
        {
            var keywords = _config.PreferredPortNameKeywords;
            if (keywords == null || keywords.Length == 0) return portNames;

            var preferred = new List<string>();
            var rest = new List<string>();

            foreach (var portName in portNames)
            {
                var isPreferred = false;
                foreach (var keyword in keywords)
                {
                    if (string.IsNullOrEmpty(keyword)) continue;
                    if (portName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) < 0) continue;

                    isPreferred = true;
                    break;
                }

                (isPreferred ? preferred : rest).Add(portName);
            }

            preferred.AddRange(rest);
            return preferred;
        }

        private void BeginConnection()
        {
            // 前の接続の残りを次の接続へ持ち込まない (仕様書 §14)。
            DrainQueues();
            Interlocked.Increment(ref _connectionEpoch);
            SetWorkerState(ChewingSensorConnectionState.Connected);
        }

        private void EndConnection(SerialLineAssembler assembler)
        {
            assembler.Reset();
            DrainQueues();

            if (Volatile.Read(ref _workerConnectionState) == (int)ChewingSensorConnectionState.Connected)
            {
                // 保留中のキャリブレーションを打ち切らせるため、切断も世代の更新として扱う。
                Interlocked.Increment(ref _connectionEpoch);
            }

            SetWorkerState(ChewingSensorConnectionState.Disconnected);
        }

        private void DrainQueues()
        {
            while (_inbound.TryDequeue(out _)) { }
            while (_outbound.TryDequeue(out _)) { }
        }

        private void SetWorkerState(ChewingSensorConnectionState state) =>
            Volatile.Write(ref _workerConnectionState, (int)state);

        private static void WaitFor(float seconds, CancellationToken ct)
        {
            if (ct.WaitHandle.WaitOne(TimeSpan.FromSeconds(seconds))) ct.ThrowIfCancellationRequested();
        }

        public void Dispose()
        {
            _workerCts?.Cancel();

            // 読み取りタイムアウトぶんで必ず戻るので、待ち切れずに落とすことはまずない。
            if (_worker != null && _worker.IsAlive) _worker.Join(TimeSpan.FromSeconds(2));
            _worker = null;

            _workerCts?.Dispose();
            _workerCts = null;

            _pending?.Complete(ChewingCalibrationResult.NotConnected());
            _pending = null;

            _onMouthEvent.Dispose();
            _connectionState.Dispose();
        }

        /// <summary>
        /// キャリブレーション要求が今どこにいるか (仕様書 §16 の Unity 側の状態)。
        /// 再送するのか、無期限に待つのか、期限を切るのかがフェーズごとに違うため区別する。
        /// </summary>
        private enum CalibrationStage
        {
            /// <summary>CAL_START を送り、CAL_ACCEPTED を待っている。</summary>
            AwaitingAccept,

            /// <summary>案内とカウントダウンの表示中。何も送らず、期限も設けない。</summary>
            Prompting,

            /// <summary>CAL_NOISE を送り、CAL_NOISE_DONE を待っている。</summary>
            MeasuringNoise,

            /// <summary>CAL_CHEW を送り、CAL_CHEW_DONE と CAL_DONE を待っている。</summary>
            MeasuringChew
        }

        /// <summary>保留中の1件のキャリブレーション要求。メインスレッドからのみ触る。</summary>
        private sealed class CalibrationRequest
        {
            private UniTaskCompletionSource _stage = new();

            public uint RequestId { get; }
            public int Epoch { get; }

            public CalibrationStage Stage { get; private set; } = CalibrationStage.AwaitingAccept;

            /// <summary>決着した結果。値が入ったらこの要求は終わっている。</summary>
            public ChewingCalibrationResult? Terminal { get; private set; }

            /// <summary>現在の段が進むか、要求が決着すると完了する。</summary>
            public UniTask StageCompleted => _stage.Task;

            public int Attempts { get; private set; }
            public float LastSentAt { get; private set; }
            public float StageStartedAt { get; private set; }

            public CalibrationRequest(uint requestId, int epoch)
            {
                RequestId = requestId;
                Epoch = epoch;
            }

            public void BeginStage(CalibrationStage stage)
            {
                Stage = stage;
                StageStartedAt = Time.realtimeSinceStartup;
                _stage = new UniTaskCompletionSource();
            }

            public void MarkSent(float now)
            {
                Attempts++;
                LastSentAt = now;
            }

            /// <summary>期待した応答が来たので次の段へ進める。</summary>
            public void CompleteStage() => _stage.TrySetResult();

            /// <summary>CAL_CHEW_DONE のように、まだ終端ではない応答で待ち時間を取り直す。</summary>
            public void RestartStageDeadline() => StageStartedAt = Time.realtimeSinceStartup;

            public void Complete(ChewingCalibrationResult result)
            {
                if (Terminal.HasValue) return;

                Terminal = result;
                _stage.TrySetResult();
            }
        }
    }
}
