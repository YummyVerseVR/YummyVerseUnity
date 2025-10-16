using System;
using PCVR.Model.Interfaces;
using UnityEngine;
using Zenject;
using GLTFast;

namespace PCVR
{
    public class FoodInstantiater : MonoBehaviour
    {
        [Inject] private IQRCodeManager QRCodeManager;
        private string endpoint = "http://localhost:8001/";

        void Start()
        {
            Uri uri = new Uri(endpoint + QRCodeManager.UserId + "/model");
            var gltfImport = new GltfImport();
            gltfImport.Load(uri);
            gltfImport.InstantiateMainSceneAsync(transform);
        }

    }
}
