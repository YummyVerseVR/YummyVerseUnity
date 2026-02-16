using R3;

namespace YummyVerse.Scripts.Model.Interface
{
    public interface ISettingManager
    {
        ReactiveProperty<bool> isStandaloneMode { get; }
    }
}