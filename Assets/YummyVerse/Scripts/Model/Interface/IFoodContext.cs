using R3;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Model.Interface
{
    public interface IFoodContext
    {
        ReactiveProperty<FoodDownloadResult> downloadResult { get; }
    }
}