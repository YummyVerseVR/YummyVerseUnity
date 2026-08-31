using Zenject;

namespace YummyVerse.Scripts.Model.DI
{
    /// <summary>Scene composition root for the Restaurant runtime.</summary>
    public sealed class RestaurantInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            RestaurantCoreBindings.Install(Container);
            RestaurantCatalogBindings.Install(Container);
            RestaurantInteractionBindings.Install(Container);
            RestaurantCommandBindings.Install(Container);
        }
    }
}
