using R3;
using TMPro;
using UnityEngine;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;
using YummyVerse.Scripts.ViewModel.Interface;
using Zenject;

namespace YummyVerse.Scripts.View.Tutorial
{
    /// <summary>
    /// 現在のステップIDと経過秒数を表示するデバッグHUD。
    /// 本番ビルドでは既定で無効になる(forceEnable で上書き可能)。
    /// </summary>
    public class TutorialDebugHudView : MonoBehaviour
    {
        [SerializeField] private GameObject hudRoot;
        [SerializeField] private TextMeshProUGUI hudText;

        [Tooltip("本番ビルドでも強制的に表示する")]
        [SerializeField] private bool forceEnable;

        private ITutorialRunner _runner;
        private IAppStateMachine _appState;

        private AppState _currentAppState = AppState.Attract;
        private string _currentStepId = string.Empty;

        [Inject]
        public void Construct(ITutorialRunner runner, IAppStateMachine appState)
        {
            _runner = runner;
            _appState = appState;
        }

        private void Start()
        {
            if (!IsEnabledInThisBuild())
            {
                if (hudRoot != null) hudRoot.SetActive(false);
                enabled = false;
                return;
            }

            if (hudRoot != null) hudRoot.SetActive(true);

            _runner.CurrentStepId.Subscribe(v => _currentStepId = v).AddTo(this);
            _appState.Current.Subscribe(v => _currentAppState = v).AddTo(this);
        }

        private void Update()
        {
            if (hudText == null) return;

            var stepId = string.IsNullOrEmpty(_currentStepId) ? "-" : _currentStepId;
            hudText.text = $"AppState : {_currentAppState}\n" +
                           $"Step     : {stepId}\n" +
                           $"Elapsed  : {_runner.CurrentStepElapsedSeconds:F1}s";
        }

        private bool IsEnabledInThisBuild()
        {
            if (forceEnable) return true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return true;
#else
            return false;
#endif
        }
    }
}
