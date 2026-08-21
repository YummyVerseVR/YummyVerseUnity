using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Model.Interface
{
    public interface ISpatialAnchorBackend
    {
        Guid CurrentUuid { get; }
        Transform CurrentAnchorTransform { get; }

        UniTask<SpatialAnchorBackendResult> LoadAsync(Guid uuid, CancellationToken cancellationToken);
        UniTask<SpatialAnchorBackendResult> ReplaceAsync(Pose pose, CancellationToken cancellationToken);
        UniTask CommitReplacementAsync();
        UniTask RollbackReplacementAsync();
    }
}
