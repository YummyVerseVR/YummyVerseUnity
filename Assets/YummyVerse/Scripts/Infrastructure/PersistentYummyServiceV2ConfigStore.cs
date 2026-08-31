using System;
using System.IO;
using UnityEngine;
using YummyVerse.Scripts.Model.Interface;

namespace YummyVerse.Scripts.Infrastructure
{
    /// <summary>JSON-backed YummyService v2 configuration under persistentDataPath.</summary>
    public sealed class PersistentYummyServiceV2ConfigStore : IYummyServiceV2ConfigStore
    {
        private const string CacheFileName = "yummy-service-v2-config.json";

        private readonly string _cachePath;

        public PersistentYummyServiceV2ConfigStore()
            : this(Path.Combine(Application.persistentDataPath, CacheFileName))
        {
        }

        public PersistentYummyServiceV2ConfigStore(string cachePath)
        {
            if (string.IsNullOrWhiteSpace(cachePath))
            {
                throw new ArgumentException("A cache path is required.", nameof(cachePath));
            }

            _cachePath = cachePath;
        }

        public bool TryLoad(out string endpointUrl, out string deviceAccessToken)
        {
            endpointUrl = string.Empty;
            deviceAccessToken = string.Empty;

            try
            {
                if (!File.Exists(_cachePath)) return false;

                var json = File.ReadAllText(_cachePath);
                var data = JsonUtility.FromJson<CachedConfig>(json);
                if (data == null) return false;

                endpointUrl = data.endpointUrl ?? string.Empty;
                deviceAccessToken = data.deviceAccessToken ?? string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"YummyService v2 configuration cache could not be loaded: {exception.GetType().Name}");
                return false;
            }
        }

        public void Save(string endpointUrl, string deviceAccessToken)
        {
            try
            {
                var directory = Path.GetDirectoryName(_cachePath);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

                var data = new CachedConfig
                {
                    endpointUrl = endpointUrl ?? string.Empty,
                    deviceAccessToken = deviceAccessToken ?? string.Empty
                };
                File.WriteAllText(_cachePath, JsonUtility.ToJson(data));
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"YummyService v2 configuration cache could not be saved: {exception.GetType().Name}");
            }
        }

        [Serializable]
        private sealed class CachedConfig
        {
            public string endpointUrl;
            public string deviceAccessToken;
        }
    }
}
