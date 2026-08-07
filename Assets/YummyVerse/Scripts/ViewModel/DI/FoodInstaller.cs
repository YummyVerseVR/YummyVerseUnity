using Zenject;

namespace YummyVerse.Scripts.ViewModel.DI
{
    /// <summary>
    /// FoodViewModel は複数の機能(チュートリアルの救済・リセット)からも参照されるため、
    /// SharedViewModelInstaller でシーンスコープにバインドしている。
    /// FoodView はサブコンテナから親を辿って同じインスタンスを受け取る。
    /// </summary>
    public class FoodInstaller  : MonoInstaller
    {
        public override void InstallBindings()
        {
        }
    }
}
