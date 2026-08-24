using System.Threading;
using Cysharp.Threading.Tasks;

namespace YummyVerse.Scripts.ViewModel.Interface
{
    public interface IFoodSelectionFlow
    {
        UniTask RunAsync(CancellationToken ct);
    }
}
