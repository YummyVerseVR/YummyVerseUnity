using System;

namespace YummyVerse.Scripts.Model.Interface
{
    /// <summary>
    /// 開いている1本のCOMポート。プロトコルは知らず、バイト列の出し入れだけを担う。
    ///
    /// 呼び出しは咀嚼計専用の受信スレッドからのみ行う想定で、スレッドセーフではない
    /// (仕様書 §15.1: メインスレッドをシリアルI/Oでブロックしない)。
    /// </summary>
    public interface ISerialPortConnection : IDisposable
    {
        string PortName { get; }

        /// <summary>
        /// 受信バッファから読み出す。設定した読み取りタイムアウトまでブロックし、
        /// 何も来なければ 0 を返す。切断は例外で通知する。
        /// </summary>
        int Read(byte[] buffer, int offset, int count);

        void Write(byte[] buffer, int offset, int count);

        /// <summary>送受信バッファを捨てる。ポートを採用した直後の残骸を消すために使う。</summary>
        void DiscardBuffers();

        /// <summary>
        /// 保留中の読み書きを打ち切る。<see cref="Read"/> は 0 を返すか例外で戻る。
        ///
        /// このメソッドだけは受信スレッド以外から呼んでよい。USB が抜けた直後の読み取りは
        /// タイムアウトで戻ってこないことがあり、終了時にスレッドを起こす手段が要るため。
        /// 既に閉じている場合や中断できない実装では何もしない (例外を投げない)。
        /// </summary>
        void CancelPendingIo();
    }
}
