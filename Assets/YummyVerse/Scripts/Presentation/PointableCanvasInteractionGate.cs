using System.Collections.Generic;
using Oculus.Interaction;
using UnityEngine;
using UnityEngine.UI;

namespace YummyVerse.Scripts.Presentation
{
    /// <summary>
    /// 表示していないワールド空間パネルの受け口を落として、レイの候補から外す。
    /// 受け口は <see cref="RayInteractable"/> / <see cref="PokeInteractable"/> /
    /// <see cref="PointableCanvas"/> / <see cref="GraphicRaycaster"/> の4種。
    /// </summary>
    /// <remarks>
    /// <para>
    /// Interaction SDK の <see cref="RayInteractor"/> は、レイが最初に当たった面を1つだけ
    /// 選ぶ (RayInteractor.ComputeCandidate)。面は板の見た目とは無関係なので、透明なパネルでも
    /// 手前に居ればその分だけ奥のパネルからレイを奪い、「見えているのに押せない領域」を作る。
    /// 一方 <see cref="PokeInteractor"/> は指が面から 3cm 以内 (PokeInteractable の
    /// _enterHoverNormal) に入って初めて反応するので、離れた場所に置き去りのパネルには
    /// 引っかからない。素手ならどこでも押せるのにコントローラのレイだけ効かない、という
    /// 非対称はここから出る。だから「閉じているパネルは受け口ごと落とす」が要る。
    /// </para>
    /// <para>
    /// 判定は毎フレームやる。パネルは <c>PlaceInFrontOfCamera</c> で動くし頭も動くので、
    /// 開閉の瞬間に一度決めるだけでは翌フレームには古くなる。
    /// </para>
    /// <para>
    /// 表示中のパネルが複数あるときは全部生かしておく。どれを触っているかはレイの当たり順
    /// (RayInteractor) が決める話で、こちらで1枚に絞ると、手前にあるパネルではなく
    /// 「原点がカメラに近いパネル」が勝ってしまい、目の前のダイアログの方が死ぬ。
    /// </para>
    /// </remarks>
    public sealed class PointableCanvasInteractionGate
    {
        private static readonly List<PointableCanvasInteractionGate> Gates = new();

        /// <summary>
        /// 受け口の二重所有を防ぐ。設定ダイアログのように、同じパネルを指す
        /// <see cref="PointableCanvasInteractionGate"/> が複数できることがあり、
        /// 放っておくと片方の Apply(true) をもう片方の Apply(false) が上書きして
        /// パネルごと沈黙する。先に取った側だけが面倒を見る。
        /// </summary>
        private static readonly HashSet<Behaviour> Owned = new();

        private static Driver _driver;

        private readonly List<Behaviour> _receivers = new();
        private readonly Transform _anchor;
        private readonly CanvasGroup _canvasGroup;
        private bool _wantsInteraction;
        private bool _applied = true; // プレハブ既定は有効。構築時に一度落として揃える。

        public PointableCanvasInteractionGate(Component context)
        {
            if (context == null) return;

            var pointableCanvas = context.GetComponentInParent<PointableCanvas>(true);
            _anchor = pointableCanvas != null ? pointableCanvas.transform : context.transform;
            _canvasGroup = context as CanvasGroup ?? context.GetComponent<CanvasGroup>();

            foreach (var interactable in _anchor.GetComponentsInChildren<IInteractable>(true))
            {
                if (interactable is Behaviour behaviour) Claim(behaviour);
            }

            foreach (var canvas in _anchor.GetComponentsInChildren<PointableCanvas>(true)) Claim(canvas);
            foreach (var raycaster in _anchor.GetComponentsInChildren<GraphicRaycaster>(true)) Claim(raycaster);

            if (_receivers.Count == 0) return;

            Gates.Add(this);
            EnsureDriver();

            // 開くまでは触らせない。ここで一度落としておかないと、一度も開閉していない
            // パネルがプレハブの有効なままレイを奪い続ける。
            Evaluate();
        }

        public void SetEnabled(bool value)
        {
            _wantsInteraction = value;
            Evaluate();
        }

        private void Claim(Behaviour receiver)
        {
            if (receiver == null || !Owned.Add(receiver)) return;
            _receivers.Add(receiver);
        }

        /// <summary>
        /// 表示していると言えるか。<see cref="CanvasGroup.blocksRaycasts"/> も見るのは、
        /// ゲートを通さず CanvasGroup だけで閉じるパネル (StandaloneWindowView など) を
        /// 取りこぼさないため。alpha は DOFade で遅れて上がるので判定には使わない。
        /// </summary>
        private bool ShouldInteract =>
            _wantsInteraction
            && _anchor != null
            && _anchor.gameObject.activeInHierarchy
            && (_canvasGroup == null || _canvasGroup.blocksRaycasts);

        private void Evaluate()
        {
            var value = ShouldInteract;
            if (value == _applied) return;
            _applied = value;

            foreach (var receiver in _receivers)
            {
                if (receiver != null) receiver.enabled = value;
            }
        }

        private static void Tick()
        {
            for (var index = Gates.Count - 1; index >= 0; index--)
            {
                var gate = Gates[index];
                if (gate._anchor == null)
                {
                    foreach (var receiver in gate._receivers) Owned.Remove(receiver);
                    Gates.RemoveAt(index);
                    continue;
                }

                gate.Evaluate();
            }
        }

        private static void EnsureDriver()
        {
            if (_driver != null) return;

            var host = new GameObject(nameof(PointableCanvasInteractionGate))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            Object.DontDestroyOnLoad(host);
            _driver = host.AddComponent<Driver>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            Gates.Clear();
            Owned.Clear();
            _driver = null;
        }

        /// <summary>毎フレームの判定を回すだけの入れ物。</summary>
        private sealed class Driver : MonoBehaviour
        {
            // パネルの配置 (PlaceInFrontOfCamera) が済んだあとに見たいので LateUpdate。
            private void LateUpdate() => Tick();
        }
    }
}
