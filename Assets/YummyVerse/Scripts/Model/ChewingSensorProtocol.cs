using System.Globalization;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Model
{
    /// <summary>
    /// YummyVerse 咀嚼計シリアル通信プロトコル v1 (YV-SERIAL-001) の文字列表現だけを担う層。
    ///
    /// ポートも状態も持たない純粋関数の集まりなので、EditMode テストでそのまま検証できる。
    /// 命令名と列挙値は大文字・小文字を区別する (仕様書 §5)。
    /// </summary>
    public static class ChewingSensorProtocol
    {
        public const string ProtocolId = "YUMMYVERSE";
        public const string ProtocolVersion = "1";
        public const string DeviceRole = "CHEWING_SENSOR";

        /// <summary>LF を除いたメッセージ本文の上限バイト数 (仕様書 §5)。</summary>
        public const int MaxBodyBytes = 63;

        public const byte Terminator = (byte)'\n';

        /// <summary>識別要求。ハンドシェイク成立前に送ってよい唯一の命令 (仕様書 §6.2)。</summary>
        public const string HelloMessage = "HELLO," + ProtocolId + "," + ProtocolVersion;

        private const string ReadyMessage =
            "READY," + ProtocolId + "," + ProtocolVersion + "," + DeviceRole;

        /// <summary>予約値。0 は「保留要求なし」の内部表現なので、電文に現れたら不正扱いにする (仕様書 §10)。</summary>
        public const uint NoRequestId = 0u;

        /// <summary>キャリブレーション開始要求。この時点ではまだ測定は始まらない (仕様書 §9.1)。</summary>
        public static string BuildCalibrationStart(uint requestId) =>
            Build("CAL_START", requestId);

        /// <summary>
        /// 測定フェーズの開始要求。咀嚼計は受信した時点で測定を始めるので、
        /// 利用者への案内が終わってから送る (仕様書 §9.2)。
        /// </summary>
        public static string BuildCalibrationPhase(ChewingCalibrationPhase phase, uint requestId) =>
            Build(phase == ChewingCalibrationPhase.Noise ? "CAL_NOISE" : "CAL_CHEW", requestId);

        /// <summary>
        /// 中断要求。咀嚼計はフェーズ指示を無期限に待つため、放棄するなら明示的に
        /// 状態を捨てさせないと次の要求が BUSY になる (仕様書 §9.5, §9.7)。
        /// </summary>
        public static string BuildCalibrationAbort(uint requestId) =>
            Build("CAL_ABORT", requestId);

        private static string Build(string command, uint requestId) =>
            command + "," + requestId.ToString(CultureInfo.InvariantCulture);

        /// <summary>
        /// 受信した1行を解釈する。解釈できない行は破棄する前提なので、例外は投げず false を返す。
        /// </summary>
        public static bool TryParse(string line, out ChewingSensorMessage message)
        {
            message = default;
            if (string.IsNullOrEmpty(line)) return false;

            var fields = line.Split(',');
            switch (fields[0])
            {
                case "READY":
                    // 役割まで一致した READY だけを適合デバイスの証拠とする (仕様書 §6.2)。
                    if (line != ReadyMessage) return false;
                    message = ChewingSensorMessage.Ready();
                    return true;

                case "CAL_ACCEPTED":
                    if (fields.Length != 2 || !TryParseRequestId(fields[1], out var acceptedId)) return false;
                    message = ChewingSensorMessage.CalibrationAccepted(acceptedId);
                    return true;

                case "CAL_NOISE_DONE":
                    if (fields.Length != 2 || !TryParseRequestId(fields[1], out var noiseDoneId)) return false;
                    message = ChewingSensorMessage.CalibrationNoiseDone(noiseDoneId);
                    return true;

                case "CAL_CHEW_DONE":
                    if (fields.Length != 2 || !TryParseRequestId(fields[1], out var chewDoneId)) return false;
                    message = ChewingSensorMessage.CalibrationChewDone(chewDoneId);
                    return true;

                case "CAL_DONE":
                    if (fields.Length != 2 || !TryParseRequestId(fields[1], out var doneId)) return false;
                    message = ChewingSensorMessage.CalibrationDone(doneId);
                    return true;

                case "CAL_FAILED":
                    if (fields.Length != 3 || !TryParseRequestId(fields[1], out var failedId)) return false;
                    if (fields[2].Length == 0) return false;
                    message = ChewingSensorMessage.CalibrationFailed(failedId, fields[2]);
                    return true;

                case "MOUTH":
                    if (fields.Length != 2 || !TryParseMouthState(fields[1], out var mouthState)) return false;
                    message = ChewingSensorMessage.Mouth(mouthState);
                    return true;

                default:
                    return false;
            }
        }

        private static bool TryParseRequestId(string field, out uint requestId)
        {
            requestId = NoRequestId;

            // uint.TryParse は "+1" や前後の空白も通してしまう。仕様は10進数字だけなので自前で弾く。
            if (field.Length == 0 || field.Length > 10) return false;
            foreach (var c in field)
            {
                if (c < '0' || c > '9') return false;
            }

            if (!uint.TryParse(field, NumberStyles.None, CultureInfo.InvariantCulture, out requestId)) return false;
            return requestId != NoRequestId;
        }

        private static bool TryParseMouthState(string field, out MouthState state)
        {
            switch (field)
            {
                case "OPEN":
                    state = MouthState.Open;
                    return true;

                // 仕様書の正式な閉口イベントは CLOSED。ファームウェア側が CLOSE と書く実装も
                // 実際にあるため、どちらも閉口として受ける (開閉いずれも咀嚼音を鳴らすので実害はない)。
                case "CLOSED":
                case "CLOSE":
                    state = MouthState.Closed;
                    return true;

                default:
                    state = default;
                    return false;
            }
        }
    }
}
