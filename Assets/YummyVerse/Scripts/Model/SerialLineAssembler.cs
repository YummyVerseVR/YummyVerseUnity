using System.Collections.Generic;
using System.Text;

namespace YummyVerse.Scripts.Model
{
    /// <summary>
    /// シリアルのバイトストリームを LF 区切りの1行へ組み直す (プロトコル仕様書 §5, §15)。
    ///
    /// 1回の Read が1メッセージである保証はなく、分割受信も複数行の一括受信も起こる。
    /// ここで境界を確定させてから解析へ渡す。
    /// </summary>
    public sealed class SerialLineAssembler
    {
        private readonly int _maxBodyBytes;
        private readonly List<byte> _buffer;

        /// <summary>本文が長すぎた行を捨てている最中か。次の LF まで読み飛ばして同期を回復する。</summary>
        private bool _resynchronizing;

        public SerialLineAssembler(int maxBodyBytes = ChewingSensorProtocol.MaxBodyBytes)
        {
            _maxBodyBytes = maxBodyBytes;
            _buffer = new List<byte>(maxBodyBytes + 1);
        }

        /// <summary>
        /// 受信バイトを流し込み、完成した行を <paramref name="lines"/> へ追加する。
        /// 空行は仕様どおり無視するため、追加されない。
        /// </summary>
        public void Append(byte[] data, int offset, int count, List<string> lines)
        {
            for (var i = offset; i < offset + count; i++)
            {
                var b = data[i];

                if (b != ChewingSensorProtocol.Terminator)
                {
                    if (_resynchronizing) continue;

                    if (_buffer.Count >= _maxBodyBytes)
                    {
                        // 長すぎる行は途中まで解釈すると別の命令に化けうる。行ごと捨てる。
                        _buffer.Clear();
                        _resynchronizing = true;
                        continue;
                    }

                    _buffer.Add(b);
                    continue;
                }

                if (_resynchronizing)
                {
                    _resynchronizing = false;
                    _buffer.Clear();
                    continue;
                }

                // CRLF で送ってくる実装に備え、LF 直前の CR だけ取り除く。
                if (_buffer.Count > 0 && _buffer[_buffer.Count - 1] == (byte)'\r')
                {
                    _buffer.RemoveAt(_buffer.Count - 1);
                }

                if (_buffer.Count > 0)
                {
                    lines.Add(Encoding.UTF8.GetString(_buffer.ToArray()));
                }

                _buffer.Clear();
            }
        }

        /// <summary>
        /// 切断・再接続時に部分受信中の行を捨てる (仕様書 §11, §14)。
        /// 前の接続の断片が次の接続の先頭へ繋がると、存在しない命令が生まれてしまう。
        /// </summary>
        public void Reset()
        {
            _buffer.Clear();
            _resynchronizing = false;
        }
    }
}
