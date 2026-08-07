using System;
using YummyVerse.Scripts.Model.Struct.SO;

namespace YummyVerse.Scripts.Model.Struct
{
    /// <summary>
    /// 来場者が選んだメニュー。
    /// 現状の食品同一性は LocalFoods(ローカル) と Guid(サーバ) の2系統しかないため、その両方を持つ。
    /// </summary>
    public readonly struct MenuItem
    {
        public LocalFoods Food { get; }
        public Guid Guid { get; }

        public MenuItem(LocalFoods food, Guid guid)
        {
            Food = food;
            Guid = guid;
        }

        public override string ToString() => $"{Food}({Guid})";
    }
}
