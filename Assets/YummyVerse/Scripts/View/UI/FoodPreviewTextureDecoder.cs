using System;
using UnityEngine;
using WebP;

namespace YummyVerse.Scripts.View.UI
{
    /// <summary>
    /// preview.png/jpg/jpeg/webp を実行時に Texture2D へ変換する。
    /// URLの拡張子ではなくWebPのRIFFヘッダーで判定するため、API URLに拡張子がなくても扱える。
    /// </summary>
    public static class FoodPreviewTextureDecoder
    {
        public static bool TryDecode(byte[] bytes, out Texture2D texture)
        {
            texture = null;
            if (bytes == null || bytes.Length == 0) return false;

            try
            {
                if (IsWebP(bytes))
                {
                    texture = Texture2DExt.CreateTexture2DFromWebP(
                        bytes,
                        lMipmaps: false,
                        lLinear: false,
                        lError: out var error);
                    if (error == Error.Success && texture != null)
                    {
                        texture.wrapMode = TextureWrapMode.Clamp;
                        return true;
                    }

                    Release(texture);
                    texture = null;
                    return false;
                }

                texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, false)
                {
                    wrapMode = TextureWrapMode.Clamp
                };
                if (ImageConversion.LoadImage(texture, bytes, true)) return true;

                Release(texture);
                texture = null;
                return false;
            }
            catch (Exception exception)
            {
                Release(texture);
                texture = null;
                Debug.LogWarning($"Food preview image could not be decoded: {exception.Message}");
                return false;
            }
        }

        public static bool IsWebP(byte[] bytes)
        {
            return bytes is { Length: >= 12 } &&
                   bytes[0] == (byte)'R' &&
                   bytes[1] == (byte)'I' &&
                   bytes[2] == (byte)'F' &&
                   bytes[3] == (byte)'F' &&
                   bytes[8] == (byte)'W' &&
                   bytes[9] == (byte)'E' &&
                   bytes[10] == (byte)'B' &&
                   bytes[11] == (byte)'P';
        }

        private static void Release(UnityEngine.Object target)
        {
            if (target == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(target);
            else UnityEngine.Object.DestroyImmediate(target);
        }
    }
}
