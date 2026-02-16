using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using YummyVerse.Scripts.Model.Struct.SO;
using Zenject;

[CreateAssetMenu(fileName = "LocalFoodSO", menuName = "Scriptable Objects/LocalFoodSO")]
public class LocalFoodSO : ScriptableObject, IInitializable
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

    public void Initialize()
    {

        foreach (var e in entries)
        {
            if (!_dict.ContainsKey(e.food))
                _dict.Add(e.food, e.Guid);
            else
                Debug.LogWarning($"Duplicate key: {e.food}");
        }
    }

    public bool TryGet(LocalFoods food, out Guid guid)
    {
        if (_dict == null)
            Initialize();
        
        var success =_dict.TryGetValue(food, out var tmp);
        guid = Guid.Parse(tmp);
        return success;
    }
}
