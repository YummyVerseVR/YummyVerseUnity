using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.Localization;

namespace YummyVerse.Scripts.ViewModel.Tutorial
{
    public static class LocalizedStringExtensions
    {
        /// <summary>
        /// LocalizedString を現在のロケールの文字列に解決する。
        /// 未設定のときは空文字を返し、呼び出し側で分岐しなくてよいようにしている。
        /// </summary>
        public static async UniTask<string> ResolveAsync(this LocalizedString self, CancellationToken ct)
        {
            if (self == null || self.IsEmpty) return string.Empty;

            var handle = self.GetLocalizedStringAsync();
            if (!handle.IsDone)
            {
                await UniTask.WaitUntil(() => handle.IsDone, cancellationToken: ct);
            }

            return handle.Result ?? string.Empty;
        }
    }
}
