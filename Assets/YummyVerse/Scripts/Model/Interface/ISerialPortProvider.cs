using System.Collections.Generic;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Model.Interface
{
    /// <summary>
    /// COMポートの列挙とオープン。プラットフォーム依存部分を Model から締め出すための境界。
    /// テストではダミー実装を差し込んで、実機なしで探索とハンドシェイクを検証できる。
    /// </summary>
    public interface ISerialPortProvider
    {
        /// <summary>アクセスできるCOMポート名を返す。1つも無ければ空。</summary>
        IReadOnlyList<string> ListPortNames();

        /// <summary>ポートを開く。開けなければ例外を投げる。</summary>
        ISerialPortConnection Open(string portName, SerialPortSettings settings);
    }
}
