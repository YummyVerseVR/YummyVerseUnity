using System.Collections.Generic;
using Oculus.Interaction;
using UnityEngine;
using UnityEngine.UI;

namespace YummyVerse.Scripts.View.UI
{
    /// <summary>
    /// ワールド空間 UI の Interactable を、表示状態と前後関係に応じて開け閉めする開閉口。
    ///
    /// CanvasGroup の alpha / blocksRaycasts は uGUI のレイキャストしか止めない。
    /// PointableCanvas の土台にある RayInteractable / PokeInteractable はそのまま生きているので、
    /// 「見えていないパネル」がコントローラのレイの当たり先として選ばれてしまい、
    /// その裏や奥にある表示中のパネルのボタンを押せなくなる。
    /// チュートリアルのパネルと設定画面はどれもカメラ前の同じ位置に出るため、これが起きる。
    ///
    /// さらに、表示中のパネルが複数あるときは <b>カメラに一番近いものだけ</b>を触れるようにする。
    /// ISDK の RayInteractor は本来一番近い当たりを選ぶ実装(RayInteractor.ComputeCandidate)だが、
    /// 同じ方向に重なって出るパネル同士では奥のパネルが選ばれてしまうことがあるため、
    /// 「見た目で手前にあるものが勝つ」をこちら側で確定させる。
    /// </summary>
    public sealed class PointableCanvasInteractionGate
    {
        /// <summary>生成済みの開閉口。前後関係の判定に全パネルを見る必要があるため静的に持つ。</summary>
        private static readonly List<PointableCanvasInteractionGate> Gates = new();

        private readonly List<MonoBehaviour> _interactables = new();

        /// <summary>
        /// ポインタの受け口。Interactable を切るだけでは、ISDK 側の候補選びから外れても
        /// PointableCanvasModule の RaycastAll には残り続けるため、これらもまとめて切る。
        /// </summary>
        private readonly List<Behaviour> _pointerReceivers = new();

        /// <summary>パネルの位置。カメラからの距離を測る基準に使う。</summary>
        private readonly Transform _anchor;

        /// <summary>パネル自身が「触れる状態になりたい」かどうか(= 表示中かどうか)。</summary>
        private bool _wantsInteraction;

        /// <param name="context">パネル側のコンポーネント。ここから PointableCanvas の根を辿る。</param>
        public PointableCanvasInteractionGate(Component context)
        {
            if (context == null) return;

            // Interactable は Canvas より上(バックプレートの根)に付いているので、
            // PointableCanvas まで遡ってからその配下を集める。
            var pointableCanvas = context.GetComponentInParent<PointableCanvas>(true);
            _anchor = pointableCanvas != null ? pointableCanvas.transform : context.transform;

            foreach (var interactable in _anchor.GetComponentsInChildren<IInteractable>(true))
            {
                if (interactable is MonoBehaviour behaviour) _interactables.Add(behaviour);
            }

            _pointerReceivers.AddRange(_anchor.GetComponentsInChildren<PointableCanvas>(true));
            _pointerReceivers.AddRange(_anchor.GetComponentsInChildren<GraphicRaycaster>(true));

            if (_interactables.Count > 0 || _pointerReceivers.Count > 0) Gates.Add(this);
        }

        /// <summary>
        /// このパネルを触れる状態にしたいかを伝える。実際に有効になるかは、
        /// 同時に表示されている他のパネルとの前後関係で決まる。
        /// </summary>
        public void SetEnabled(bool value)
        {
            _wantsInteraction = value;
            Reevaluate();
        }

        /// <summary>表示中のパネルのうち、カメラに一番近いものだけを有効にする。</summary>
        private static void Reevaluate()
        {
            // 破棄済みのパネルを掃除する(ドメインリロードを切っていると再生をまたいで残る)。
            Gates.RemoveAll(gate => gate._anchor == null);

            var cameraTransform = Camera.main != null ? Camera.main.transform : null;

            PointableCanvasInteractionGate frontMost = null;
            var nearestDistance = float.MaxValue;

            foreach (var gate in Gates)
            {
                if (!gate._wantsInteraction) continue;

                // カメラが取れないときは前後を決めようがないので、表示中のものは全て有効に倒す。
                if (cameraTransform == null)
                {
                    frontMost = null;
                    break;
                }

                var distance = Vector3.Distance(cameraTransform.position, gate._anchor.position);
                if (distance >= nearestDistance) continue;

                nearestDistance = distance;
                frontMost = gate;
            }

            foreach (var gate in Gates)
            {
                gate.Apply(gate._wantsInteraction && (frontMost == null || gate == frontMost));
            }
        }

        private void Apply(bool value)
        {
            foreach (var interactable in _interactables)
            {
                // OnEnable/OnDisable 側で ISDK のレジストリ登録・解除が行われる。
                if (interactable != null) interactable.enabled = value;
            }

            // PointableCanvas を切ると PointableCanvasModule への登録ごと外れ、
            // GraphicRaycaster を切ると EventSystem.RaycastAll の対象からも外れる。
            // 見た目(Canvas の描画)はそのまま残るので、奥のダイアログは見えたまま反応しなくなる。
            foreach (var receiver in _pointerReceivers)
            {
                if (receiver != null) receiver.enabled = value;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            Gates.Clear();
        }
    }
}
