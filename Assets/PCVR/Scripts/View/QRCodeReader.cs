using System;
using PCVR.Model.Interfaces;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
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
    
        void Start()
        {
            // カメラの初期化
            InitializeCamera();
            
            // ZXingのBarcodeReaderを初期化
            barcodeReader = new BarcodeReader();
        }
    
        void Update()
        {
            if (!isCameraReady)
            {
                return;
            }
    
        
            // WebCamTextureからピクセルデータを取得
            Color32[] pixels = webCamTexture.GetPixels32();
            
            // QRコードのデコードを試みる
            Result result = barcodeReader.Decode(pixels, webCamTexture.width, webCamTexture.height);

            if (result != null)
            {
                // デコードに成功した場合、テキストを更新
                resultText.text = result.Text;
                Assert.IsNotNull(_qrCodeManager);
                Assert.IsNotNull(_qrCodeManager.UserId);
                _qrCodeManager.UserId.Value = Guid.Parse(result.Text);
                SceneManager.LoadScene("PCVRTest");
            }
        }
    
        /// <summary>
        /// Webカメラを初期化して再生を開始します。
        /// </summary>
        private void InitializeCamera()
        {
            WebCamDevice[] devices = WebCamTexture.devices;
    
            if (devices.Length == 0)
            {
                Debug.LogError("使用可能なWebカメラが見つかりません。");
                resultText.text = "カメラが見つかりません";
                return;
            }
    
            // 最初のカメラを使用する
            webCamTexture = new WebCamTexture(devices[0].name);
            cameraFeedRawImage.texture = webCamTexture;
            webCamTexture.Play();
            isCameraReady = true;
    
            // RawImageのアスペクト比をカメラ映像に合わせる
            StartCoroutine(AdjustAspectRatio());
        }
    
        /// <summary>
        /// カメラ映像のアスペクト比に合わせてRawImageのサイズを調整します。
        /// </summary>
        private System.Collections.IEnumerator AdjustAspectRatio()
        {
            // カメラの初期化を待つ
            yield return new WaitUntil(() => webCamTexture.width > 100);
    
            AspectRatioFitter aspectRatioFitter = cameraFeedRawImage.GetComponent<AspectRatioFitter>();
            if (aspectRatioFitter == null)
            {
                aspectRatioFitter = cameraFeedRawImage.gameObject.AddComponent<AspectRatioFitter>();
            }
            aspectRatioFitter.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
            aspectRatioFitter.aspectRatio = (float)webCamTexture.width / (float)webCamTexture.height;
        }
    
        void OnDestroy()
        {
            // アプリケーション終了時にカメラを停止
            if (webCamTexture != null && webCamTexture.isPlaying)
            {
                webCamTexture.Stop();
            }
        }
    }
}
