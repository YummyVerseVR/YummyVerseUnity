using Cysharp.Threading.Tasks;
using UnityEngine;
using YummyVerse.Scripts.Presentation;
using Zenject;

namespace YummyVerse.Scripts.View
{
    /// <summary>Lifecycle adapter for the placement-preview collaborator.</summary>
    public sealed class FoodPlacementCubeView : MonoBehaviour
    {
        private FoodPlacementPreviewController _controller;

        [Inject]
        public void Construct(FoodPlacementPreviewController controller)
        {
            _controller = controller;
        }

        private void Start()
        {
            _controller.Initialize(transform, this.GetCancellationTokenOnDestroy());
            _controller.SetHostEnabled(isActiveAndEnabled);
        }

        private void OnEnable()
        {
            _controller?.SetHostEnabled(true);
        }

        private void OnDisable()
        {
            _controller?.SetHostEnabled(false);
        }

        private void LateUpdate()
        {
            _controller?.Tick();
        }

        private void OnDestroy()
        {
            _controller?.Dispose();
        }
    }
}
