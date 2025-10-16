using System;
using System.Collections;
using PCVR.Model.Interfaces;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zenject;
using ZXing;

namespace PCVR.View
{
    public class QRCodeReader : MonoBehaviour
    {
        [Inject] private readonly IQRCodeManager _qrCodeManager;

        [Tooltip("Webカメラの映像を表示するRawImage")]
        public RawImage cameraFeedRawImage;

        [Tooltip("読み取ったQRコードの値を表示するText")]
        public TextMeshProUGUI resultText;

        private WebCamTexture webCamTexture;
        private BarcodeReader barcodeReader;
        private bool isCameraReady = false;

        private void Start()
        {
            barcodeReader = new BarcodeReader();
            StartCoroutine(InitializeCameraCoroutine());
        }

        private void Update()
        {
            if (!isCameraReady || webCamTexture == null || !webCamTexture.isPlaying)
                return;

            if (webCamTexture.width < 100 || webCamTexture.height < 100)
                return;

            try
            {
                var pixels = webCamTexture.GetPixels32();
                var result = barcodeReader.Decode(pixels, webCamTexture.width, webCamTexture.height);
                if (result != null)
                {
                    resultText.text = result.Text;
                    _qrCodeManager.UserId.Value = Guid.Parse(result.Text);
                    SceneManager.LoadScene("PCVRTest");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"QRコード解析中にエラー: {e.Message}");
            }
        }

        private IEnumerator InitializeCameraCoroutine()
        {
            WebCamDevice[] devices = WebCamTexture.devices;
            if (devices.Length == 0)
            {
                Debug.LogError("使用可能なWebカメラが見つかりません。");
                resultText.text = "カメラが見つかりません";
                yield break;
            }

            // もし裏面カメラがある場合、フロントではなく裏面を選ぶ
            string selectedDeviceName = devices[0].name;
            foreach (var device in devices)
            {
                if (!device.isFrontFacing)
                {
                    selectedDeviceName = device.name;
                    break;
                }
            }

            // WebCamTexture生成（明示的な解像度指定を追加）
            webCamTexture = new WebCamTexture(selectedDeviceName, 1280, 720, 30);

            // RawImageに反映
            cameraFeedRawImage.texture = webCamTexture;
            cameraFeedRawImage.material = null; // マテリアル干渉防止
            cameraFeedRawImage.color = Color.white; // 黒背景を防ぐ

            webCamTexture.Play();

            // 実際にフレームが届くまで待つ
            yield return new WaitUntil(() => webCamTexture.didUpdateThisFrame && webCamTexture.width > 100);

            isCameraReady = true;
            Debug.Log($"WebCam started: {webCamTexture.width}x{webCamTexture.height} ({selectedDeviceName})");

            // アスペクト比補正
            StartCoroutine(AdjustAspectRatio());
        }

        private IEnumerator AdjustAspectRatio()
        {
            yield return new WaitUntil(() => webCamTexture.width > 100);

            AspectRatioFitter aspectRatioFitter = cameraFeedRawImage.GetComponent<AspectRatioFitter>();
            if (aspectRatioFitter == null)
                aspectRatioFitter = cameraFeedRawImage.gameObject.AddComponent<AspectRatioFitter>();

            aspectRatioFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            aspectRatioFitter.aspectRatio = (float)webCamTexture.width / webCamTexture.height;

            // 映像の反転対応（多くのWebカメラで必要）
            cameraFeedRawImage.rectTransform.localScale = new Vector3(
                webCamTexture.videoVerticallyMirrored ? -1 : 1,
                1,
                1
            );
        }

        private void OnDestroy()
        {
            if (webCamTexture != null && webCamTexture.isPlaying)
                webCamTexture.Stop();
        }
    }
}
