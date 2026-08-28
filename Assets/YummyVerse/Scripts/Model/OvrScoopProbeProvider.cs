using System.Collections.Generic;
using UnityEngine;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Model
{
    /// <summary>
    /// Meta XR SDK の手の位置をアプリケーション層から隔離する境界。
    ///
    /// OVRCameraRig の leftHandAnchor / rightHandAnchor は、コントローラーを持っていれば
    /// コントローラーの、ハンドトラッキング中は手の姿勢を追従する (手が優先される)。
    /// そのため「コントローラー、ハンドトラッキングの場合は手」という要求をそのまま満たす。
    /// </summary>
    public sealed class OvrScoopProbeProvider : IScoopProbeProvider
    {
        private readonly List<ScoopProbe> _probes = new(2);
        private OVRCameraRig _rig;
        private bool _rigMissingLogged;

        public IReadOnlyList<ScoopProbe> GetProbes(float radius)
        {
            _probes.Clear();

            var rig = ResolveRig();
            if (rig == null) return _probes;

            TryAppend(rig.leftHandAnchor, OVRInput.Handedness.LeftHanded, ScoopHand.Left, radius);
            TryAppend(rig.rightHandAnchor, OVRInput.Handedness.RightHanded, ScoopHand.Right, radius);
            return _probes;
        }

        private void TryAppend(Transform anchor, OVRInput.Handedness handedness, ScoopHand hand, float radius)
        {
            if (anchor == null || !IsTracked(handedness)) return;
            _probes.Add(new ScoopProbe(hand, anchor.position, radius));
        }

        /// <summary>
        /// 追跡が切れている間、アンカーは最後の姿勢のまま止まる。
        /// そこで判定してしまうと触っていないのにすくいが成立するため、有効性を確かめる。
        /// </summary>
        private static bool IsTracked(OVRInput.Handedness handedness)
        {
            var active = OVRInput.GetActiveControllerForHand(handedness);
            if (active != OVRInput.Controller.None && OVRInput.GetControllerPositionValid(active)) return true;

            var isLeft = handedness == OVRInput.Handedness.LeftHanded;
            return OVRInput.GetControllerPositionValid(isLeft ? OVRInput.Controller.LHand : OVRInput.Controller.RHand)
                   || OVRInput.GetControllerPositionValid(isLeft ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch);
        }

        private OVRCameraRig ResolveRig()
        {
            if (_rig != null) return _rig;

            _rig = Object.FindFirstObjectByType<OVRCameraRig>();
            if (_rig == null && !_rigMissingLogged)
            {
                _rigMissingLogged = true;
                Debug.LogWarning("[Eating] OVRCameraRig が見つからないため、手による すくい判定を行えません。");
            }

            return _rig;
        }
    }
}
