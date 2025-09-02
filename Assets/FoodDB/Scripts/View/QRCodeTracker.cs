using System;
using UnityEngine;
using ZXing.Unity;

public class QRCodeTracker : MonoBehaviour
{
    [Header("Camera Settings")]
    private WebCamTexture webCamTexture;
    private IBarcodeReader barcodeReader;

    [Header("Anchor Prefab")]
    public GameObject qrAnchorPrefab;
    private GameObject qrAnchorInstance;

    void Start()
    {
        // WebCamTexture の初期化
        if (WebCamTexture.devices.Length > 0)
        {
            string camName = WebCamTexture.devices[0].name;
            webCamTexture = new WebCamTexture(camName, 1280, 720, 30);
            webCamTexture.Play();
        }
        else
        {
            Debug.LogError("No camera detected!");
            return;
        }

        // ZXing QRコードリーダー
        barcodeReader = new BarcodeReader();

        // アンカーオブジェクト生成
        if (qrAnchorPrefab != null)
        {
            qrAnchorInstance = Instantiate(qrAnchorPrefab);
            qrAnchorInstance.SetActive(false);
        }
    }

    void Update()
    {
        if (webCamTexture == null || !webCamTexture.isPlaying) return;

        try
        {
            // 現在のフレームからQRコード解析
            var result = barcodeReader.Decode(
                webCamTexture.GetPixels32(),
                webCamTexture.width,
                webCamTexture.height
            );

            if (result != null)
            {
                Debug.Log("QR Content: " + result.Text);

                // QRコードの位置をスクリーン座標として取得
                // ZXingのResultPointsは画像ピクセル座標
                if (result.ResultPoints != null && result.ResultPoints.Length > 0)
                {
                    // QRコードの中心を計算
                    float avgX = 0f, avgY = 0f;
                    foreach (var p in result.ResultPoints)
                    {
                        avgX += p.X;
                        avgY += p.Y;
                    }
                    avgX /= result.ResultPoints.Length;
                    avgY /= result.ResultPoints.Length;

                    // ピクセル座標 → スクリーン座標
                    Vector3 screenPos = new Vector3(avgX, webCamTexture.height - avgY, 0);

                    // スクリーン座標 → ワールド座標 (カメラ前方にレイキャスト)
                    Ray ray = Camera.main.ScreenPointToRay(screenPos);

                    // 環境にコリジョンがあるならヒット座標を利用
                    if (Physics.Raycast(ray, out RaycastHit hit, 5f))
                    {
                        PlaceAnchor(hit.point, Quaternion.LookRotation(hit.normal));
                    }
                    else
                    {
                        // ヒットしなければ、カメラ前方1mに仮置き
                        Vector3 fallback = ray.origin + ray.direction * 1.0f;
                        PlaceAnchor(fallback, Camera.main.transform.rotation);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("QR decode error: " + ex.Message);
        }
    }

    private void PlaceAnchor(Vector3 pos, Quaternion rot)
    {
        if (qrAnchorInstance != null)
        {
            qrAnchorInstance.transform.position = pos;
            qrAnchorInstance.transform.rotation = rot;
            qrAnchorInstance.SetActive(true);
        }
    }
}
