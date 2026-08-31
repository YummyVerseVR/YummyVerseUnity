using System.Collections.Generic;
using NUnit.Framework;
using YummyVerse.Scripts.Model;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Editor.Tests
{
    /// <summary>
    /// プロトコル仕様書 YV-SERIAL-001 §5〜§11 の受け入れ判定。
    /// 適合確認チェックリスト (§18) のうち、文字列解釈で決まる項目をここで固定する。
    /// </summary>
    public class ChewingSensorProtocolTests
    {
        [Test]
        public void Hello_MatchesSpecification()
        {
            Assert.That(ChewingSensorProtocol.HelloMessage, Is.EqualTo("HELLO,YUMMYVERSE,1"));
        }

        [Test]
        public void CalibrationStart_CarriesRequestId()
        {
            Assert.That(ChewingSensorProtocol.BuildCalibrationStart(42u), Is.EqualTo("CAL_START,42"));
        }

        [Test]
        public void Ready_IsAcceptedOnlyWithProtocolAndRole()
        {
            Assert.That(ChewingSensorProtocol.TryParse("READY,YUMMYVERSE,1,CHEWING_SENSOR", out var message), Is.True);
            Assert.That(message.Kind, Is.EqualTo(ChewingSensorMessageKind.Ready));

            // 他のシリアル機器が返しそうな似た応答は適合デバイスの証拠にしない。
            Assert.That(ChewingSensorProtocol.TryParse("READY,YUMMYVERSE,1", out _), Is.False);
            Assert.That(ChewingSensorProtocol.TryParse("READY,YUMMYVERSE,1,OTHER_DEVICE", out _), Is.False);
            Assert.That(ChewingSensorProtocol.TryParse("READY,OTHER,1,CHEWING_SENSOR", out _), Is.False);
        }

        [Test]
        public void CommandNames_AreCaseSensitive()
        {
            Assert.That(ChewingSensorProtocol.TryParse("ready,YUMMYVERSE,1,CHEWING_SENSOR", out _), Is.False);
            Assert.That(ChewingSensorProtocol.TryParse("MOUTH,open", out _), Is.False);
        }

        [Test]
        public void CalibrationResponses_CarryRequestId()
        {
            Assert.That(ChewingSensorProtocol.TryParse("CAL_ACCEPTED,42", out var accepted), Is.True);
            Assert.That(accepted.Kind, Is.EqualTo(ChewingSensorMessageKind.CalibrationAccepted));
            Assert.That(accepted.RequestId, Is.EqualTo(42u));

            Assert.That(ChewingSensorProtocol.TryParse("CAL_DONE,4294967295", out var done), Is.True);
            Assert.That(done.RequestId, Is.EqualTo(uint.MaxValue));

            Assert.That(ChewingSensorProtocol.TryParse("CAL_FAILED,43,SENSOR_UNSTABLE", out var failed), Is.True);
            Assert.That(failed.Kind, Is.EqualTo(ChewingSensorMessageKind.CalibrationFailed));
            Assert.That(failed.RequestId, Is.EqualTo(43u));
            Assert.That(failed.FailureReason, Is.EqualTo("SENSOR_UNSTABLE"));
        }

        [Test]
        public void RequestId_RejectsReservedAndMalformedValues()
        {
            // 0 は「保留要求なし」の予約値なので電文には現れない (§10)。
            Assert.That(ChewingSensorProtocol.TryParse("CAL_DONE,0", out _), Is.False);
            Assert.That(ChewingSensorProtocol.TryParse("CAL_DONE,+1", out _), Is.False);
            Assert.That(ChewingSensorProtocol.TryParse("CAL_DONE, 1", out _), Is.False);
            Assert.That(ChewingSensorProtocol.TryParse("CAL_DONE,4294967296", out _), Is.False);
            Assert.That(ChewingSensorProtocol.TryParse("CAL_DONE,", out _), Is.False);
        }

        [Test]
        public void Mouth_AcceptsBothClosedSpellings()
        {
            Assert.That(ChewingSensorProtocol.TryParse("MOUTH,OPEN", out var open), Is.True);
            Assert.That(open.MouthState, Is.EqualTo(MouthState.Open));

            Assert.That(ChewingSensorProtocol.TryParse("MOUTH,CLOSED", out var closed), Is.True);
            Assert.That(closed.MouthState, Is.EqualTo(MouthState.Closed));

            // ファームウェア実装のゆれを吸収する。開閉いずれも咀嚼音のトリガーで扱いは同じ。
            Assert.That(ChewingSensorProtocol.TryParse("MOUTH,CLOSE", out var shortClosed), Is.True);
            Assert.That(shortClosed.MouthState, Is.EqualTo(MouthState.Closed));
        }

        [Test]
        public void UnknownAndMalformedLines_AreRejectedWithoutThrowing()
        {
            foreach (var line in new List<string> { null, "", "PING", "MOUTH", "MOUTH,OPEN,EXTRA", "CAL_FAILED,43" })
            {
                Assert.That(ChewingSensorProtocol.TryParse(line, out _), Is.False, $"入力: {line}");
            }
        }
    }
}
