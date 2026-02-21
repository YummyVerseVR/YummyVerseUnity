using R3;
using YummyVerse.Scripts.Model.Interface;

namespace YummyVerse.Scripts.Model
{
    public class SettingManager : ISettingManager
    {
        public ReactiveProperty<bool> isStandaloneMode { get; } = new();
    }
}