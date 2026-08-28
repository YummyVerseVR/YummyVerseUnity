using UnityEngine;

namespace YummyVerse.Scripts.Presentation
{
    /// <summary>
    /// Calculates and applies the custom virtual-keyboard pose relative to the
    /// configuration panel. Runtime-controlled Near/Far modes intentionally leave the
    /// transform untouched.
    /// </summary>
    public sealed class VirtualKeyboardPlacement
    {
        private OVRVirtualKeyboard.KeyboardPosition _positionMode;
        private Transform _panelAnchor;
        private Transform _fallbackAnchor;
        private Vector3 _positionOffset;
        private float _tiltAngle;
        private float _scale;

        public void Configure(
            OVRVirtualKeyboard.KeyboardPosition positionMode,
            Transform panelAnchor,
            Transform fallbackAnchor,
            Vector3 positionOffset,
            float tiltAngle,
            float scale)
        {
            _positionMode = positionMode;
            _panelAnchor = panelAnchor;
            _fallbackAnchor = fallbackAnchor;
            _positionOffset = positionOffset;
            _tiltAngle = tiltAngle;
            _scale = scale;
        }

        public void GetInitialPose(out Vector3 position, out Quaternion rotation)
        {
            if (_panelAnchor == null)
            {
                Debug.LogWarning("VirtualKeyboardPlacement: panelAnchor が未設定です。");
                position = _fallbackAnchor != null ? _fallbackAnchor.position : Vector3.zero;
                rotation = _fallbackAnchor != null ? _fallbackAnchor.rotation : Quaternion.identity;
                return;
            }

            // パネルのスケールを持ち込まないよう、position + rotation * offset で組む。
            position = _panelAnchor.position + _panelAnchor.rotation * _positionOffset;
            rotation = _panelAnchor.rotation * Quaternion.Euler(_tiltAngle, 0f, 0f);
        }

        public void Apply(Transform keyboardTransform)
        {
            if (_positionMode != OVRVirtualKeyboard.KeyboardPosition.Custom ||
                keyboardTransform == null ||
                _panelAnchor == null)
            {
                return;
            }

            GetInitialPose(out var position, out var rotation);
            keyboardTransform.SetPositionAndRotation(position, rotation);
            keyboardTransform.localScale = Vector3.one * _scale;
        }
    }
}
