using UnityEngine;

namespace YummyVerse.Scripts.Model.Struct.SO
{
    /// <summary>
    /// 咀嚼計まわりの現場調整値。プロトコル仕様書 §13 の推奨初期値を既定にしてある。
    /// センサー実装側の最大キャリブレーション時間が判明したら completion 側を合わせること。
    /// </summary>
    [CreateAssetMenu(fileName = "ChewingSensorConfig", menuName = "YummyVerse/Chewing Sensor Config")]
    public class ChewingSensorConfig : ScriptableObject
    {
        [Header("シリアル通信 (仕様書 §4)")]
        [Tooltip("ボーレート。USB CDC では実効速度に影響しない実装もあるが、互換のため規定値を送る。")]
        [SerializeField] private int baudRate = SerialPortSettings.DefaultBaudRate;

        [Tooltip("1回の読み取りが最大どれだけブロックするか。短いほど送信キューの掃き出しが速い。")]
        [SerializeField, Min(10)] private int readTimeoutMilliseconds = 100;

        [Header("COMポート探索 (仕様書 §6, §13)")]
        [Tooltip("HELLO の再送間隔。ポートを開いた瞬間のデバイスリセットで初回が消えることがある。")]
        [SerializeField, Min(0.05f)] private float helloRetryIntervalSeconds = 0.5f;

        [Tooltip("1つの候補ポートに費やす上限。超えたら閉じて次の候補へ進む。")]
        [SerializeField, Min(0.5f)] private float portProbeTimeoutSeconds = 5f;

        [Tooltip("全ポートを試して見つからなかったときに、次の探索まで待つ時間。")]
        [SerializeField, Min(0.5f)] private float rediscoverIntervalSeconds = 3f;

        [Tooltip("この語を含むポートを先に試す。探索順の最適化にだけ使い、除外はしない (仕様書 §6.2)。")]
        [SerializeField] private string[] preferredPortNameKeywords =
        {
            "usbmodem", "usbserial", "ttyACM", "ttyUSB"
        };

        [Header("キャリブレーション (仕様書 §9, §13)")]
        [Tooltip("CAL_ACCEPTED を待つ時間。超えたら同じ requestId で CAL_START を再送する。")]
        [SerializeField, Min(0.1f)] private float calibrationAcceptedTimeoutSeconds = 1f;

        [Tooltip("CAL_START の送信回数の上限。使い切ったら要求失敗としてUIへ返す。")]
        [SerializeField, Min(1)] private int calibrationStartAttempts = 5;

        [Tooltip("CAL_NOISE 送信後に CAL_NOISE_DONE を待つ時間 (ノイズ測定時間＋安全余裕)。")]
        [SerializeField, Min(1f)] private float calibrationNoiseTimeoutSeconds = 30f;

        [Tooltip("CAL_CHEW 送信後に CAL_CHEW_DONE / CAL_DONE を待つ時間 (咀嚼測定時間＋安全余裕)。")]
        [SerializeField, Min(1f)] private float calibrationChewTimeoutSeconds = 30f;

        [Tooltip("キャリブレーション開始前に、咀嚼計へ繋がるのを待つ上限。" +
                 "超えたらキャリブレーションを飛ばしてチュートリアルを続行する。")]
        [SerializeField, Min(0f)] private float connectionWaitSeconds = 10f;

        [Header("咀嚼音")]
        [Tooltip("食品ごとの咀嚼音が無いときに鳴らす音。" +
                 "ローカル食品はフォルダ内の audio.[mp3/wav/ogg]、API v2 食品はエンドポイントの音声を優先する。" +
                 "未設定のまま音を持たない食品を出すと、その食品では咀嚼音が鳴らない。")]
        [SerializeField] private AudioClip fallbackChewSound;

        [SerializeField, Range(0f, 1f)] private float chewSoundVolume = 1f;

        public int BaudRate => baudRate;
        public int ReadTimeoutMilliseconds => readTimeoutMilliseconds;
        public float HelloRetryIntervalSeconds => helloRetryIntervalSeconds;
        public float PortProbeTimeoutSeconds => portProbeTimeoutSeconds;
        public float RediscoverIntervalSeconds => rediscoverIntervalSeconds;
        public string[] PreferredPortNameKeywords => preferredPortNameKeywords;
        public float CalibrationAcceptedTimeoutSeconds => calibrationAcceptedTimeoutSeconds;
        public int CalibrationStartAttempts => calibrationStartAttempts;
        public float CalibrationNoiseTimeoutSeconds => calibrationNoiseTimeoutSeconds;
        public float CalibrationChewTimeoutSeconds => calibrationChewTimeoutSeconds;
        public float ConnectionWaitSeconds => connectionWaitSeconds;
        public AudioClip FallbackChewSound => fallbackChewSound;
        public float ChewSoundVolume => chewSoundVolume;

        public SerialPortSettings ToSerialPortSettings() =>
            new(baudRate, readTimeoutMilliseconds);
    }
}
