using System.Collections.Generic;
using Oculus.Interaction;
using UnityEngine;
using UnityEngine.UI;

namespace YummyVerse.Scripts.Presentation
{
    /// <summary>
    /// Enables pointer receivers only on the nearest visible world-space panel.
    /// CanvasGroup alone does not remove Meta Interaction SDK receivers from candidate
    /// selection, so this collaborator owns that presentation-specific coordination.
    /// </summary>
    public sealed class PointableCanvasInteractionGate
    {
        private static readonly List<PointableCanvasInteractionGate> Gates = new();
        private readonly List<MonoBehaviour> _interactables = new();
        private readonly List<Behaviour> _pointerReceivers = new();
        private readonly Transform _anchor;
        private bool _wantsInteraction;

        public PointableCanvasInteractionGate(Component context)
        {
            if (context == null) return;

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

        public void SetEnabled(bool value)
        {
            _wantsInteraction = value;
            Reevaluate();
        }

        private static void Reevaluate()
        {
            Gates.RemoveAll(gate => gate._anchor == null);

            var cameraTransform = Camera.main != null ? Camera.main.transform : null;
            PointableCanvasInteractionGate frontMost = null;
            var nearestDistance = float.MaxValue;

            foreach (var gate in Gates)
            {
                if (!gate._wantsInteraction) continue;

                // If the camera is not available, keep the old permissive behavior.
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
                if (interactable != null) interactable.enabled = value;
            }

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
