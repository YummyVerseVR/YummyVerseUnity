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
