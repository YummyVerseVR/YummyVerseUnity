using R3;
using UnityEngine;

namespace Food3DModel.Interface
{
    public interface IFood3DModelViewModel
    {
        ReactiveProperty<GameObject> Food3DModel { get; }
    }
}