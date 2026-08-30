using System;
using System.Collections.Generic;
using System.IO;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Infrastructure
{
    /// <summary>
    /// OS のシリアルポートを <see cref="ISerialPortProvider"/> として提供する。
    ///
    /// Unity 6 の .NET Standard 2.1 プロファイルには System.IO.Ports が含まれないため
    /// (API Compatibility Level を .NET Framework へ落とすと R3 の netstandard2.1 参照が壊れる)、
    /// 必要な範囲だけを OS API へ直接つないでいる。
    /// 対応は Windows (PCVR の実行環境) と macOS / Linux (エディタでの動作確認) のみ。
    /// </summary>
    public sealed class SerialPortProvider : ISerialPortProvider
    {
        public IReadOnlyList<string> ListPortNames()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            return WindowsSerialPort.ListPortNames();
#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX || UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
            return PosixSerialPort.ListPortNames();
#else
            // Quest 単体実行 (Android) など、USB シリアルを扱えないプラットフォーム。
            // 咀嚼計なしとして扱い、探索を空振りさせる。
            return Array.Empty<string>();
#endif
        }

        public ISerialPortConnection Open(string portName, SerialPortSettings settings)
        {
            if (string.IsNullOrEmpty(portName)) throw new ArgumentException("ポート名が空です", nameof(portName));

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            return new WindowsSerialPort(portName, settings);
#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX || UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
            return new PosixSerialPort(portName, settings);
#else
            throw new IOException($"このプラットフォームではシリアルポート {portName} を開けません");
#endif
        }
    }
}
