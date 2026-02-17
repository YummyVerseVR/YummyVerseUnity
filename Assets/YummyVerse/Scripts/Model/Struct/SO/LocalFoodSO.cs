using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using YummyVerse.Scripts.Model.Struct.SO;
using Zenject;

[CreateAssetMenu(fileName = "LocalFoodSO", menuName = "Scriptable Objects/LocalFoodSO")]
public class LocalFoodSO : ScriptableObject
{
    [Serializable]
    public class LocalFoodEntry
    {
        public LocalFoods food;
        public string Guid;
    }
    
    [SerializeField]
    private List<LocalFoodEntry> entries = new();

    private Dictionary<LocalFoods, string> _dict  = new();

    private Dictionary<Guid, LocalFoods> _foodict = new();

    private void InitializeDict()
    {
        foreach (var e in entries)
        {
            if (!_dict.ContainsKey(e.food))
                _dict.Add(e.food, e.Guid);
            else
                Debug.LogWarning($"Duplicate key: {e.food}");
        }
    }

    private void InitializeFoodict()
    {
        _foodict.Clear();
        foreach (var e in entries)
        {
            if (!Guid.TryParse(e.Guid, out var guid))
            {
                Debug.LogWarning($"Invalid GUID for {e.food}: {e.Guid}");
                continue;
            }

            if (!_foodict.ContainsKey(guid))
                _foodict.Add(guid, e.food);
            else
                Debug.LogWarning($"Duplicate GUID: {e.Guid}");
        }
    }

    public bool TryGetGuid(LocalFoods food, out Guid guid)
    {
        if (_dict.Count == 0) InitializeDict();
        var success =_dict.TryGetValue(food, out var tmp);
        guid = success ? Guid.Parse(tmp) :  Guid.Empty;
        return success;
    }

    public bool TryGetLocalFood(Guid guid, out LocalFoods food)
    {
        if (_foodict.Count == 0) InitializeFoodict();
        var success = _foodict.TryGetValue(guid, out var tmp);
        food = success ? tmp :  default(LocalFoods);
        return success;
    }
}
