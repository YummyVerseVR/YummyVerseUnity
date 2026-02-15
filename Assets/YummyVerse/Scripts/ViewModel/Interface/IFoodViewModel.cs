namespace YummyVerse.Scripts.ViewModel.Interface
{
    public interface IFoodViewModel
    {
        ReactiveProperty<GltfImport> foodGltf { get; }
        ReactiveProperty<Transform> foodTransform { get; }
    }
}