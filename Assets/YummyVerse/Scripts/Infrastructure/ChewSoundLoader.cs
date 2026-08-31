using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace YummyVerse.Scripts.Infrastructure
{
    /// <summary>
    /// 食品ごとの咀嚼音を AudioClip として読み込む。
    ///
    /// 読み込みに失敗しても food 自体の表示は続けたいので、例外は投げず null を返す。
    /// 呼び出し側は null を「この食品には咀嚼音が無い」として扱い、既定音へフォールバックする。
    /// </summary>
    internal static class ChewSoundLoader
    {
        /// <summary>v2 が咀嚼音 artifact (WAV) を返すときの media type。</summary>
        private const string AudioMediaType = "audio/wav";

        /// <summary>ローカルファイルから読み込む。</summary>
        public static async UniTask<AudioClip> LoadFromFileAsync(string path, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;

            try
            {
                if (!File.Exists(path)) return null;
                if (!TryResolveAudioType(path, out var audioType))
                {
                    Debug.LogWarning($"[ChewingSensor] 対応していない咀嚼音の形式です: {path}");
                    return null;
                }

                return await LoadAsync(
                    new Uri(path).AbsoluteUri,
                    audioType,
                    null,
                    requireAudioMediaType: false,
                    ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                // 咀嚼音が読めないだけで食品の表示まで諦めない。
                Debug.LogWarning($"[ChewingSensor] 咀嚼音を読み込めませんでした ({exception.GetType().Name}): {path}");
                return null;
            }
        }

        /// <summary>
        /// URL から読み込む。GLB と違い、鳴らせなくても致命的ではないので
        /// キャッシュもハッシュ検証も挟まず、その場でデコードする。
        /// </summary>
        /// <param name="bearerToken">
        /// 認証が要る経路 (Unity Device の artifact download など) で送る token。
        /// 認証不要の public sample を読むときは null でよい。
        /// </param>
        public static async UniTask<AudioClip> LoadFromUrlAsync(
            string url, string bearerToken, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return null;
            if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp) return null;

            try
            {
                // v2 の咀嚼音は device artifact download が audio/wav で、URL に拡張子が
                // 付かない。decoder の種類は WAV を仮定するが、応答の Content-Type は
                // 後段で必ず検証する。
                var audioType = TryResolveAudioType(uri.AbsolutePath, out var resolved) ? resolved : AudioType.WAV;
                return await LoadAsync(
                    uri.ToString(),
                    audioType,
                    bearerToken,
                    requireAudioMediaType: true,
                    ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[ChewingSensor] 咀嚼音を読み込めませんでした ({exception.GetType().Name}): {url}");
                return null;
            }
        }

        private static async UniTask<AudioClip> LoadAsync(
            string uri,
            AudioType audioType,
            string bearerToken,
            bool requireAudioMediaType,
            CancellationToken ct)
        {
            using var request = UnityWebRequestMultimedia.GetAudioClip(uri, audioType);
            request.timeout = 30;
            request.SetRequestHeader("Accept", AudioMediaType);

            if (!string.IsNullOrWhiteSpace(bearerToken))
            {
                request.SetRequestHeader("Authorization", $"Bearer {bearerToken}");
            }

            // 一気に鳴らし直すので、ストリーミングではなく完全に読み込んでから使う。
            if (request.downloadHandler is DownloadHandlerAudioClip handler) handler.streamAudio = false;

            try
            {
                await request.SendWebRequest().WithCancellation(ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (UnityWebRequestException)
            {
                Debug.LogWarning($"[ChewingSensor] 咀嚼音を取得できませんでした: {uri} ({request.responseCode})");
                return null;
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[ChewingSensor] 咀嚼音を取得できませんでした: {uri} ({request.result})");
                return null;
            }

            var contentType = request.GetResponseHeader("Content-Type");
            if (requireAudioMediaType
                && (string.IsNullOrWhiteSpace(contentType)
                    || !contentType.StartsWith(AudioMediaType, StringComparison.OrdinalIgnoreCase)))
            {
                Debug.LogWarning($"[ChewingSensor] WAV 以外の応答を受信しました: {uri} ({contentType})");
                return null;
            }

            var clip = DownloadHandlerAudioClip.GetContent(request);
            if (clip == null || clip.loadState == AudioDataLoadState.Failed)
            {
                Debug.LogWarning($"[ChewingSensor] 咀嚼音をデコードできませんでした: {uri}");
                return null;
            }

            clip.name = "ChewSound";
            return clip;
        }

        private static bool TryResolveAudioType(string pathOrUrl, out AudioType audioType)
        {
            switch (Path.GetExtension(pathOrUrl).ToLowerInvariant())
            {
                case ".wav":
                    audioType = AudioType.WAV;
                    return true;
                case ".ogg":
                    audioType = AudioType.OGGVORBIS;
                    return true;
                case ".mp3":
                    audioType = AudioType.MPEG;
                    return true;
                default:
                    audioType = AudioType.UNKNOWN;
                    return false;
            }
        }
    }
}
