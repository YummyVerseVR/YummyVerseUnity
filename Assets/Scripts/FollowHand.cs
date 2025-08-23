using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

public class FollowHand : MonoBehaviour
{
    // 追従対象の手を指定（true = 右手、false = 左手）
    public bool rightHand = true;

    // 追従対象の関節（Palm, IndexTip など）
    public XRHandJointID jointId = XRHandJointID.Palm;

    // 手の座標に加える位置オフセット
    public Vector3 localOffset;

    // 手の回転に加える角度オフセット
    public Vector3 localEulerOffset;

    // 補間スピード（大きいほど追従が速くなる）
    public float smooth = 20f;

    XRHandSubsystem _sub;

    void OnEnable()
    {
        // XRHandSubsystem を取得
        var list = new List<XRHandSubsystem>();
        SubsystemManager.GetSubsystems(list);
        if (list.Count > 0) _sub = list[0];
        if (_sub != null) _sub.updatedHands += OnUpdatedHands;
    }

    void OnDisable()
    {
        if (_sub != null) _sub.updatedHands -= OnUpdatedHands;
    }

    // ハンド情報が更新されたときに呼ばれる処理
    void OnUpdatedHands(XRHandSubsystem subsystem,
        XRHandSubsystem.UpdateSuccessFlags ok,
        XRHandSubsystem.UpdateType type)
    {
        var hand = rightHand ? _sub.rightHand : _sub.leftHand;
        if (!hand.isTracked) return;

        // 指定した関節の情報を取得
        var j = hand.GetJoint(jointId);

        // 関節の姿勢（位置・回転）が取れなければ処理しない
        if (!j.TryGetPose(out Pose p)) return;

        var targetPos = p.position + p.rotation * localOffset;
        var targetRot = p.rotation * Quaternion.Euler(localEulerOffset);

        // 現在位置から目標位置へ補間
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * smooth);

        // 現在の回転から目標回転へ補間
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * smooth);
    }
}