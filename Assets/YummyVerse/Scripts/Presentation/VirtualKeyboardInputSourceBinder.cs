using System;
using System.Collections.Generic;
using UnityEngine;

namespace YummyVerse.Scripts.Presentation
{
    /// <summary>
    /// Connects the Meta virtual keyboard to the active controller and hand sources.
    /// The lookup and temporary interactor-anchor ownership are kept out of the view.
    /// </summary>
    public sealed class VirtualKeyboardInputSourceBinder : IDisposable
    {
        private readonly List<GameObject> _interactorAnchors = new();
        private bool _disposed;

        public void Bind(OVRVirtualKeyboard keyboard)
        {
            if (_disposed || keyboard == null) return;

            var cameraRig = UnityEngine.Object.FindAnyObjectByType<OVRCameraRig>();
            if (cameraRig != null)
            {
                keyboard.leftControllerRootTransform = cameraRig.leftControllerAnchor;
                keyboard.rightControllerRootTransform = cameraRig.rightControllerAnchor;
                keyboard.leftControllerDirectTransform = CreateInteractorAnchor(
                    cameraRig.leftControllerAnchor,
                    "KeyboardInteractorAnchorLeft");
                keyboard.rightControllerDirectTransform = CreateInteractorAnchor(
                    cameraRig.rightControllerAnchor,
                    "KeyboardInteractorAnchorRight");
            }
            else
            {
                Debug.LogWarning(
                    "VirtualKeyboardInputSourceBinder: OVRCameraRig が見つからないため、コントローラー入力を接続できません。");
            }

            // OVRHand.HandType は internal なので、併設の OVRSkeleton から左右を判定する。
            foreach (var hand in UnityEngine.Object.FindObjectsByType<OVRHand>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (!hand.TryGetComponent<OVRSkeleton>(out var skeleton)) continue;

                switch (skeleton.GetSkeletonType())
                {
                    case OVRSkeleton.SkeletonType.HandLeft:
                    case OVRSkeleton.SkeletonType.XRHandLeft:
                        keyboard.handLeft = hand;
                        break;
                    case OVRSkeleton.SkeletonType.HandRight:
                    case OVRSkeleton.SkeletonType.XRHandRight:
                        keyboard.handRight = hand;
                        break;
                }
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            foreach (var anchor in _interactorAnchors)
            {
                if (anchor != null) UnityEngine.Object.Destroy(anchor);
            }

            _interactorAnchors.Clear();
        }

        private Transform CreateInteractorAnchor(Transform parent, string name)
        {
            if (parent == null) return null;

            var anchor = new GameObject(name).transform;
            anchor.SetParent(parent, false);
            anchor.localPosition = new Vector3(0f, 0f, 0.062f);
            anchor.localRotation = Quaternion.identity;
            _interactorAnchors.Add(anchor.gameObject);
            return anchor;
        }
    }
}
