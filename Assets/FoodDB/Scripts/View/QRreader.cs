// using System;
// using Food3DModel.Interface;
// using UnityEngine;
// using UnityEngine.UI;
// using TMPro;
// using Zenject;
// using ZXing;
// using UnityEngine.XR.ARFoundation;
// using UnityEngine.XR.ARSubsystems;
// using Unity.Collections;
// using ZXing.Unity;
// using IBarcodeReader = ZXing.IBarcodeReader;
//
// namespace Food3DModel.View
// {
//     public class QRreader : MonoBehaviour
//     {
//         [Inject] private IQRViewModel _viewModel;
//
//         [SerializeField] private TextMeshPro resultText; // 読み取り結果を表示
//         [SerializeField] private Renderer targetRenderer; // パススルー映像を表示するオブジェクト
//         [SerializeField] private RenderTexture renderTexture; // 描画先RT
//         [SerializeField] private ARCameraManager cameraManager; // AR Foundationのカメラ
//
//         private IBarcodeReader barcodeReader = new BarcodeReader();
//
//         private void Awake()
//         {
//             // レンダラーにRenderTextureを割り当て
//             if (targetRenderer != null && renderTexture != null)
//             {
//                 targetRenderer.material.mainTexture = renderTexture;
//             }
//
//             Application.targetFrameRate = 120;
//         }
//
//         private void OnEnable()
//         {
//             if (cameraManager != null)
//                 cameraManager.frameReceived += OnCameraFrameReceived;
//         }
//
//         private void OnDisable()
//         {
//             if (cameraManager != null)
//                 cameraManager.frameReceived -= OnCameraFrameReceived;
//         }
//
//         private void OnCameraFrameReceived(ARCameraFrameEventArgs args)
//         {
//             if (!cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
//                 return;
//
//             var conversionParams = new XRCpuImage.ConversionParams
//             {
//                 inputRect = new RectInt(0, 0, image.width, image.height),
//                 outputDimensions = new Vector2Int(image.width, image.height),
//                 outputFormat = TextureFormat.RGBA32,
//                 transformation = XRCpuImage.Transformation.MirrorY
//             };
//
//             // NativeArrayで確保
//             int size = image.GetConvertedDataSize(conversionParams);
//             var rawTextureData = new NativeArray<byte>(size, Allocator.Temp);
//
//             image.Convert(conversionParams, rawTextureData);
//             image.Dispose();
//
//             // ZXing用にbyte[]に変換
//             var rawBytes = rawTextureData.ToArray();
//             rawTextureData.Dispose();
//
//             var result = barcodeReader.Decode(
//                 rawBytes,
//                 conversionParams.outputDimensions.x,
//                 conversionParams.outputDimensions.y,
//                 RGBLuminanceSource.BitmapFormat.RGBA32);
//
//             if (result != null)
//             {
//                 _viewModel.SetQRValue(result.Text);
//                 resultText.text = "Read val: " + result.Text;
//                 Debug.Log("QRコード検出: " + result.Text);
//             }
//             else
//             {
//                 resultText.text = "Read val: None";
//             }
//         }
//     }
// }
