using System.Collections.Generic;
using Oculus.Interaction;
using UnityEngine;

namespace YummyVerse.Scripts.View.UI
{
    /// <summary>
    /// 非表示中のワールド空間 UI が Interaction SDK のポインタを奪わないようにするための開閉口。
    ///
    /// CanvasGroup の alpha / blocksRaycasts は uGUI のレイキャストしか止めない。
    /// PointableCanvas の土台にある RayInteractable / PokeInteractable はそのまま生きているので、
    /// 「見えていないパネル」がコントローラのレイの当たり先として選ばれてしまい、
    /// その裏や奥にある表示中のパネルのボタンを押せなくなる。
    /// チュートリアルのパネルと設定画面はどれもカメラ前の同じ位置に出るため、これが起きる。
    ///
    /// 表示/非表示の切り替えに合わせて Interactable ごと有効・無効にして、
    /// 「見えているパネルだけが触れる」状態を保つ。
    /// </summary>
    public sealed class PointableCanvasInteractionGate
    {
        private readonly List<MonoBehaviour> _interactables = new();

        /// <param name="context">パネル側のコンポーネント。ここから PointableCanvas の根を辿る。</param>
        public PointableCanvasInteractionGate(Component context)
        {
            if (context == null) return;

            // Interactable は Canvas より上(バックプレートの根)に付いているので、
            // PointableCanvas まで遡ってからその配下を集める。
            var pointableCanvas = context.GetComponentInParent<PointableCanvas>(true);
            var root = pointableCanvas != null ? pointableCanvas.transform : context.transform;

            foreach (var interactable in root.GetComponentsInChildren<IInteractable>(true))
            {
                if (interactable is MonoBehaviour behaviour) _interactables.Add(behaviour);
            }
        }

        public void SetEnabled(bool value)
        {
            foreach (var interactable in _interactables)
            {
                // OnEnable/OnDisable 側で ISDK のレジストリ登録・解除が行われる。
                if (interactable != null) interactable.enabled = value;
            }
        }
    }
}
