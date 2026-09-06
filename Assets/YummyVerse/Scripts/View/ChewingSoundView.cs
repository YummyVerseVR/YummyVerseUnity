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
    /// 咀嚼計の開閉イベントと、食べ物を1回すくったタイミングで、表示中の食品の咀嚼音を鳴らす。
    ///
    /// すくいでも鳴らすのは、咀嚼計が繋がっていない展示でも口に運ぶ手応えを返すため。
    /// 経路ごとに鳴らし方を変えると重なったときの挙動が読みにくくなるので、
    /// どちらも同じ「頭から鳴らし直す」1つの入口に集約する。
    ///
    /// プロトコル v1 では OPEN と CLOSED を区別せず、どちらも「1回噛んだ」として同じ音を鳴らす。
    /// 再生途中に次のイベントが来たら重ねずに頭から鳴らし直す。噛むテンポと音のテンポを
    /// 一致させたいので、PlayOneShot による重ね合わせは使わない。
    ///
    /// 鳴らす音は食品ごとに差し替わる。音を持たない食品(built-in food など)では
    /// ChewingSensorConfig の既定音を使う。
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class ChewingSoundView : MonoBehaviour
    {
        private IChewingSensorService _sensor;
        private IGameEventBus _gameEventBus;
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
            IChewingSensorService sensor, IGameEventBus gameEventBus,
            IFoodViewModel foodViewModel, ChewingSensorConfig config)
        {
            _sensor = sensor;
            _gameEventBus = gameEventBus;
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

            _sensor.OnMouthEvent.Subscribe(_ => PlayFromStart()).AddTo(this);

            // すくった瞬間にも1回噛んだぶんの音を返す。
            _gameEventBus.GetStream(GameEventId.FoodScooped)
                .Subscribe(_ => PlayFromStart()).AddTo(this);
        }

        private void SetFoodChewSound(AudioClip clip)
        {
            if (ReferenceEquals(clip, _loadedClip) && _audioSource.clip != null) return;

            _audioSource.Stop();
            _audioSource.clip = clip != null ? clip : _config.FallbackChewSound;

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
                // 食品にも設定にも音が無い。毎イベント警告すると邪魔なので黙って何もしない。
                return;
            }

            // Stop() を挟まずに Play() だけだと再生位置が引き継がれる環境があるため、明示的に巻き戻す。
            _audioSource.Stop();
            _audioSource.time = 0f;
            _audioSource.Play();
        }
    }
}
