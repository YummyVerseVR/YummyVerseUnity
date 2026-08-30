using R3;
using UnityEngine;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct.SO;
using Zenject;

namespace YummyVerse.Scripts.View
{
    /// <summary>
    /// 咀嚼計の開閉イベントで咀嚼音を鳴らす。
    ///
    /// プロトコル v1 では OPEN と CLOSED を区別せず、どちらも「1回噛んだ」として同じ音を鳴らす。
    /// 再生途中に次のイベントが来たら重ねずに頭から鳴らし直す。噛むテンポと音のテンポを
    /// 一致させたいので、PlayOneShot による重ね合わせは使わない。
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class ChewingSoundView : MonoBehaviour
    {
        private IChewingSensorService _sensor;
        private ChewingSensorConfig _config;
        private AudioSource _audioSource;

        [Inject]
        public void Construct(IChewingSensorService sensor, ChewingSensorConfig config)
        {
            _sensor = sensor;
            _config = config;
        }

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.loop = false;

            // 咀嚼音は本人の口の中の音なので、頭の向きで音量が変わらない 2D で鳴らす。
            _audioSource.spatialBlend = 0f;
        }

        /// <summary>
        /// 実行時に生成される View なので、Awake は Construct より先に走る。
        /// 注入済みの設定を使う初期化はここで行う。
        /// </summary>
        private void Start()
        {
            _audioSource.clip = _config.ChewSound;
            _audioSource.volume = _config.ChewSoundVolume;

            if (_config.ChewSound == null)
            {
                Debug.LogWarning("[ChewingSensor] 咀嚼音が未設定です。ChewingSensorConfig の Chew Sound を割り当ててください。");
            }

            _sensor.OnMouthEvent.Subscribe(_ => PlayFromStart()).AddTo(this);
        }

        private void PlayFromStart()
        {
            if (_audioSource.clip == null) return;

            // Stop() を挟まずに Play() だけだと再生位置が引き継がれる環境があるため、明示的に巻き戻す。
            _audioSource.Stop();
            _audioSource.time = 0f;
            _audioSource.Play();
        }
    }
}
