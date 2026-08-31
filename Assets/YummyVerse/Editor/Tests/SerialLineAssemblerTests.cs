using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using YummyVerse.Scripts.Model;

namespace YummyVerse.Editor.Tests
{
    /// <summary>
    /// プロトコル仕様書 §5 / §15 の行組み立て。
    /// 「分割受信と複数行の一括受信の両方を正しく処理できる」(§18) の裏付け。
    /// </summary>
    public class SerialLineAssemblerTests
    {
        [Test]
        public void SplitReads_AreJoinedIntoOneLine()
        {
            var assembler = new SerialLineAssembler();
            var lines = new List<string>();

            Feed(assembler, "MOU", lines);
            Assert.That(lines, Is.Empty);

            Feed(assembler, "TH,OP", lines);
            Assert.That(lines, Is.Empty);

            Feed(assembler, "EN\n", lines);
            Assert.That(lines, Is.EqualTo(new[] { "MOUTH,OPEN" }));
        }

        [Test]
        public void MultipleLinesInOneRead_AreAllReturned()
        {
            var assembler = new SerialLineAssembler();
            var lines = new List<string>();

            Feed(assembler, "CAL_ACCEPTED,1\nCAL_DONE,1\nMOUTH,OPEN\n", lines);

            Assert.That(lines, Is.EqualTo(new[] { "CAL_ACCEPTED,1", "CAL_DONE,1", "MOUTH,OPEN" }));
        }

        [Test]
        public void TrailingCarriageReturn_IsStripped()
        {
            var assembler = new SerialLineAssembler();
            var lines = new List<string>();

            Feed(assembler, "MOUTH,CLOSED\r\n", lines);

            Assert.That(lines, Is.EqualTo(new[] { "MOUTH,CLOSED" }));
        }

        [Test]
        public void EmptyLines_AreIgnored()
        {
            var assembler = new SerialLineAssembler();
            var lines = new List<string>();

            Feed(assembler, "\n\r\nMOUTH,OPEN\n", lines);

            Assert.That(lines, Is.EqualTo(new[] { "MOUTH,OPEN" }));
        }

        [Test]
        public void OverlongBody_IsDiscardedAndSyncRecoversAtNextTerminator()
        {
            var assembler = new SerialLineAssembler();
            var lines = new List<string>();

            Feed(assembler, new string('X', ChewingSensorProtocol.MaxBodyBytes + 10) + "\nMOUTH,OPEN\n", lines);

            Assert.That(lines, Is.EqualTo(new[] { "MOUTH,OPEN" }));
        }

        [Test]
        public void Reset_DropsPartiallyReceivedLine()
        {
            var assembler = new SerialLineAssembler();
            var lines = new List<string>();

            Feed(assembler, "MOU", lines);
            assembler.Reset();
            Feed(assembler, "TH,OPEN\n", lines);

            // 前の接続の断片が繋がって "MOUTH,OPEN" に化けてはいけない。
            Assert.That(lines, Is.EqualTo(new[] { "TH,OPEN" }));
        }

        private static void Feed(SerialLineAssembler assembler, string text, List<string> lines)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            assembler.Append(bytes, 0, bytes.Length, lines);
        }
    }
}
