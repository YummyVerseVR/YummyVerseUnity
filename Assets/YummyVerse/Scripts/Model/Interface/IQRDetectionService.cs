using System;
using R3;
using UnityEngine;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Model.Interface
{
    public interface IQRDetectionService
    {
        ReactiveProperty<Guid> OnChangeGUID { get; }
        ReactiveProperty<Transform> OnChangeTransform { get; }

        /// <summary>
        /// QRの認識がロストしたときに発火する。
        /// </summary>
        Observable<Unit> OnLost { get; }

        void NotifyDetectQR(Guid guid, Transform transform);

        /// <summary>
        /// Standalone UIなど、物理QRの検出を伴わずに食べ物GUIDだけを変更する。
        /// QR検出イベントや位置情報は更新しない。
        /// </summary>
        void NotifyFoodGuid(Guid guid);

        /// <summary>
        /// QRの認識がロストしたことを通知する。
        /// </summary>
        void NotifyLostQR();

        /// <summary>
        /// セッション終了時に認識状態を初期化する。
        /// ここに漏れがあると2人目の来場者で前の人の食べ物が残る。
        /// </summary>
        void Reset();
    }
}
