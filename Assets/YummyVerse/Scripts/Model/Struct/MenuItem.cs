using System;
using YummyVerse.Scripts.Model.Struct.SO;

namespace YummyVerse.Scripts.Model.Struct
{
    /// <summary>
    /// 既存 Standalone catalog から来場者が選んだローカル食品。
    /// Guid は端末内 catalog の内部 ID であり、QR payload や YummyService の order identity ではない。
    /// Network item は v2 transport と unified catalog が確定するまでこの型へ詰め替えない。
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
