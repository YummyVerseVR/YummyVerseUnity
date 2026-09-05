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

        /// <summary>
        /// 受信スレッドが今開いているポート。終了時に、ブロックしている読み取りを
        /// メインスレッドから打ち切るためだけに公開する (仕様書 §15.1 の例外)。
        /// Dispose するのは受信スレッド側だけ。所有権はここでは移さない。
        /// </summary>
        private volatile ISerialPortConnection _activeConnection;

        /// <summary>
        /// 直前に咀嚼計だと確認できたポート。次の探索でここから試す。
        /// 受信スレッドからのみ触る。
        /// </summary>
        private string _lastGoodPortName;

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

        public UniTask<ChewingCalibrationResult> CalibrateAsync(Action onAccepted, CancellationToken ct)
        {
            if (_connectionState.Value != ChewingSensorConnectionState.Connected)
            {
                Debug.LogWarning("[ChewingSensor] 咀嚼計へ接続していないため、キャリブレーションを要求できません");
                return UniTask.FromResult(ChewingCalibrationResult.NotConnected());
            }

            if (_pending != null)
            {
                // 仕様書 §9.1: Unity が同時に保留できる要求は1件だけ。
                Debug.LogWarning("[ChewingSensor] 既にキャリブレーションを要求中のため、新しい要求は受け付けません");
                return UniTask.FromResult(ChewingCalibrationResult.Failed("BUSY"));
            }

            var request = new CalibrationRequest(
                _requestIds.Next(), onAccepted, Volatile.Read(ref _connectionEpoch));
            _pending = request;

            SendCalibrationStart(request);
            return WaitForCalibrationAsync(request, ct);
        }

        private async UniTask<ChewingCalibrationResult> WaitForCalibrationAsync(
            CalibrationRequest request, CancellationToken ct)
        {
            try
            {
                return await request.Completion.Task.AttachExternalCancellation(ct);
            }
            finally
            {
                if (ReferenceEquals(_pending, request)) _pending = null;
            }
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
                case ChewingSensorMessageKind.CalibrationDone:
                case ChewingSensorMessageKind.CalibrationFailed:
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
                    pending.MarkAccepted();
                    return;

                case ChewingSensorMessageKind.CalibrationDone:
                    Complete(pending, ChewingCalibrationResult.Succeeded());
                    return;

                case ChewingSensorMessageKind.CalibrationFailed:
                    Debug.LogWarning($"[ChewingSensor] キャリブレーションに失敗しました: {message.FailureReason}");
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

            if (!pending.IsAccepted)
            {
                if (now - pending.LastSentAt < _config.CalibrationAcceptedTimeoutSeconds) return;

                if (pending.Attempts >= Mathf.Max(1, _config.CalibrationStartAttempts))
                {
                    Debug.LogWarning("[ChewingSensor] CAL_ACCEPTED が返らないため、キャリブレーションを断念します");
                    Complete(pending, ChewingCalibrationResult.TimedOut());
                    return;
                }

                SendCalibrationStart(pending);
                return;
            }

            if (now - pending.AcceptedAt < _config.CalibrationCompletionTimeoutSeconds) return;

            Debug.LogWarning("[ChewingSensor] CAL_DONE / CAL_FAILED が返らないため、キャリブレーションを断念します");
            Complete(pending, ChewingCalibrationResult.TimedOut());
        }

        private void SendCalibrationStart(CalibrationRequest request)
        {
            request.MarkSent(Time.realtimeSinceStartup);
            Send(ChewingSensorProtocol.BuildCalibrationStart(request.RequestId));
        }

        private void Complete(CalibrationRequest request, ChewingCalibrationResult result)
        {
            if (ReferenceEquals(_pending, request)) _pending = null;
            request.Completion.TrySetResult(result);
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
                    // 参照を先に外してから閉じる。逆順にすると、閉じ終わったポートに対して
                    // Dispose 側が打ち切りを掛けにいく窓が広がる。
                    _activeConnection = null;
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
                    _activeConnection = connection;
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

                            // 次に切れたときはこのポートから試す。USB の再列挙で
                            // 番号が変わらない限り、全ポートを舐め直さずに済む。
                            _lastGoodPortName = portName;

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
                    if (connection != null)
                    {
                        _activeConnection = null;
                        connection.Dispose();
                    }
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
        ///
        /// 優先順位は「直前に当たったポート → 名前がキーワードに合うポート → 残り」。
        /// 先頭の1件が効くのは切断からの復帰で、USB の再列挙でポート番号が変わらない限り
        /// 全ポートへ HELLO を投げ直さずに済む。Quest Link の抜き差しでも再列挙は起きるので、
        /// 咀嚼計と無関係な着脱で無駄な総なめをしないためにも要る。
        ///
        /// キーワード照合が効くのは POSIX のデバイスファイル名 (/dev/cu.usbmodem*) だけで、
        /// Windows の "COMn" には当たらない。Windows 側の並べ替えは
        /// <see cref="Infrastructure.SerialPortProvider"/> の実装 (NT デバイス名による種別判定) が担う。
        /// </summary>
        private IEnumerable<string> OrderCandidates(IReadOnlyList<string> portNames)
        {
            var keywords = _config.PreferredPortNameKeywords;
            var lastGood = _lastGoodPortName;

            var first = new List<string>();
            var preferred = new List<string>();
            var rest = new List<string>();

            foreach (var portName in portNames)
            {
                if (lastGood != null && string.Equals(portName, lastGood, StringComparison.OrdinalIgnoreCase))
                {
                    first.Add(portName);
                    continue;
                }

                (MatchesKeyword(portName, keywords) ? preferred : rest).Add(portName);
            }

            first.AddRange(preferred);
            first.AddRange(rest);
            return first;
        }

        private static bool MatchesKeyword(string portName, string[] keywords)
        {
            if (keywords == null) return false;

            foreach (var keyword in keywords)
            {
                if (string.IsNullOrEmpty(keyword)) continue;
                if (portName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }

            return false;
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

            // USB が抜けた直後の読み取りは、読み取りタイムアウトを過ぎても戻らないことがある。
            // キャンセルを見てもらう前に、まずブロックしている I/O を叩き起こす。
            // ここで Dispose しないのは所有権を受信スレッドに残すためで、
            // 閉じるのはあちらの finally に任せる。
            try
            {
                _activeConnection?.CancelPendingIo();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ChewingSensor] 受信の打ち切りに失敗しました: {e.Message}");
            }

            if (_worker != null && _worker.IsAlive) _worker.Join(TimeSpan.FromSeconds(2));
            _worker = null;

            _workerCts?.Dispose();
            _workerCts = null;

            _pending?.Completion.TrySetResult(ChewingCalibrationResult.NotConnected());
            _pending = null;

            _onMouthEvent.Dispose();
            _connectionState.Dispose();
        }

        /// <summary>保留中の1件のキャリブレーション要求。メインスレッドからのみ触る。</summary>
        private sealed class CalibrationRequest
        {
            private readonly Action _onAccepted;

            public uint RequestId { get; }
            public int Epoch { get; }
            public UniTaskCompletionSource<ChewingCalibrationResult> Completion { get; } = new();

            public int Attempts { get; private set; }
            public float LastSentAt { get; private set; }
            public bool IsAccepted { get; private set; }
            public float AcceptedAt { get; private set; }

            public CalibrationRequest(uint requestId, Action onAccepted, int epoch)
            {
                RequestId = requestId;
                Epoch = epoch;
                _onAccepted = onAccepted;
            }

            public void MarkSent(float now)
            {
                Attempts++;
                LastSentAt = now;
            }

            public void MarkAccepted()
            {
                // 重複した CAL_ACCEPTED (再送への応答) で案内文を出し直さない。
                if (IsAccepted) return;

                IsAccepted = true;
                AcceptedAt = Time.realtimeSinceStartup;
                _onAccepted?.Invoke();
            }
        }
    }
}
