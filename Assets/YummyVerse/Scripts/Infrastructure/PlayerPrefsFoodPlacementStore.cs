using UnityEngine;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Infrastructure
{
    public sealed class PlayerPrefsFoodPlacementStore : IFoodPlacementStore
    {
        private const string PlacementKey = "YummyVerse.FoodPlacement.v1";

        public bool TryLoad(out FoodPlacementData data)
        {
            data = default;
            if (!PlayerPrefs.HasKey(PlacementKey)) return false;

            var json = PlayerPrefs.GetString(PlacementKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json)) return false;

            try
            {
                data = JsonUtility.FromJson<FoodPlacementData>(json);
                if (data.IsValid()) return true;

                Debug.LogWarning("[FoodPlacement] Saved placement is incomplete or invalid.");
                data = default;
                return false;
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"[FoodPlacement] Failed to read saved placement: {exception.Message}");
                data = default;
                return false;
            }
        }

        public void Save(FoodPlacementData data)
        {
            if (!data.IsValid())
            {
                throw new System.ArgumentException("Food placement data is invalid.", nameof(data));
            }

            PlayerPrefs.SetString(PlacementKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }
    }
}
