using System;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.Networking;

namespace PCVR.Scripts.View
{
    public class ControllerController : MonoBehaviour
    {

        private string endpoint = "http://localhost:8001/ping";
        private ReactiveProperty<bool> isEating = new ReactiveProperty<bool>(false);

        private void Start()
        {
            NotifyEating().Forget();
            isEating.Where(v => v).Subscribe(v =>
            {
                NotifyEating().Forget();
            });
        }

        private void Update()
        {
            if ((Camera.main.transform.position - this.transform.position).magnitude < 0.15f)
            {
                isEating.Value = true;
            }
        }

        private async UniTask NotifyEating()
        {
            UnityWebRequest request = UnityWebRequest.Get(endpoint);
            await request.SendWebRequest();
            Debug.Log(request.downloadHandler.text);
        }
    }

}