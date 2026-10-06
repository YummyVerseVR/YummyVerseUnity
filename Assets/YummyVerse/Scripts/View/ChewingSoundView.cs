using R3;
using UnityEngine;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;
using YummyVerse.Scripts.Model.Struct.SO;
using YummyVerse.Scripts.ViewModel.Interface;
using Zenject;

namespace YummyVerse.Scripts.View
{
    /// <summary>
    /// 圧力センサーの CLOSED イベントで、表示中の食品の咀嚼音を1回再生する。
    /// 次の咀嚼では重ねずに頭から再生し直す。
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class ChewingSoundView : MonoBehaviour
    {
        private IChewingSensorService _sensor;
        private IFoodViewModel _foodViewModel;
        private ChewingSensorConfig _config;
        private AudioSource _audioSource;

        /// <summary>
        /// 実行時に生成した AudioClip。アセットではないので、差し替え時に自分で破棄する。
        /// 展示は無人で長時間動くため、来場者ごとに1つずつ溜めていくと効いてくる。
        /// </summary>
        private AudioClip _loadedClip;

        [Inject]
        public void Construct(
            IChewingSensorService sensor,
            IFoodViewModel foodViewModel, ChewingSensorConfig config)
        {
            _sensor = sensor;
            _foodViewModel = foodViewModel;
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
        /// 注入済みの依存を使う初期化はここで行う。
        /// </summary>
        private void Start()
        {
            _audioSource.volume = _config.ChewSoundVolume;

            // 食品が切り替わったら音も差し替える。鳴っている途中なら止める。
            // 前の食品の音がそのまま次の食品で鳴り続ける方が違和感が大きい。
            _foodViewModel.chewSound.Subscribe(SetFoodChewSound).AddTo(this);

            _sensor.OnMouthEvent.Where(state => state == MouthState.Closed)
                .Subscribe(_ => PlayFromStart()).AddTo(this);

        }

        private void SetFoodChewSound(AudioClip clip)
        {
            if (ReferenceEquals(clip, _loadedClip) && _audioSource.clip != null) return;

            _audioSource.Stop();
            _audioSource.clip = clip != null ? clip : _config.FallbackChewSound;
            Debug.Log($"[ChewAudio] Clip ready: {_audioSource.clip?.name ?? "none"}");

            ReleaseLoadedClip(clip);
            _loadedClip = clip;
        }

        /// <summary>直前の食品ぶんの AudioClip を捨てる。既定音はアセットなので触らない。</summary>
        private void ReleaseLoadedClip(AudioClip keep)
        {
            if (_loadedClip == null) return;
            if (ReferenceEquals(_loadedClip, keep)) return;
            if (ReferenceEquals(_loadedClip, _config.FallbackChewSound)) return;

            Destroy(_loadedClip);
            _loadedClip = null;
        }

        private void OnDestroy()
        {
            ReleaseLoadedClip(null);
        }

        private void PlayFromStart()
        {
            if (_audioSource.clip == null)
            {
                Debug.LogWarning("[ChewAudio] CLOSED received but no audio clip is selected.");
                return;
            }

            // Stop() を挟まずに Play() だけだと再生位置が引き継がれる環境があるため、明示的に巻き戻す。
            _audioSource.Stop();
            _audioSource.time = 0f;
            _audioSource.Play();
            Debug.Log($"[ChewAudio] Play: clip={_audioSource.clip.name}, volume={_audioSource.volume}, listenerVolume={AudioListener.volume}, paused={AudioListener.pause}, playing={_audioSource.isPlaying}");
        }
    }
}
