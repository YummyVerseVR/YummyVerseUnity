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
        public void CalibrationPhaseRequests_MatchSpecification()
        {
            Assert.That(
                ChewingSensorProtocol.BuildCalibrationPhase(ChewingCalibrationPhase.Noise, 42u),
                Is.EqualTo("CAL_NOISE,42"));
            Assert.That(
                ChewingSensorProtocol.BuildCalibrationPhase(ChewingCalibrationPhase.Chew, 42u),
                Is.EqualTo("CAL_CHEW,42"));
            Assert.That(ChewingSensorProtocol.BuildCalibrationAbort(42u), Is.EqualTo("CAL_ABORT,42"));
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

            Assert.That(ChewingSensorProtocol.TryParse("CAL_NOISE_DONE,42", out var noiseDone), Is.True);
            Assert.That(noiseDone.Kind, Is.EqualTo(ChewingSensorMessageKind.CalibrationNoiseDone));
            Assert.That(noiseDone.RequestId, Is.EqualTo(42u));

            Assert.That(ChewingSensorProtocol.TryParse("CAL_CHEW_DONE,42", out var chewDone), Is.True);
            Assert.That(chewDone.Kind, Is.EqualTo(ChewingSensorMessageKind.CalibrationChewDone));
            Assert.That(chewDone.RequestId, Is.EqualTo(42u));

            Assert.That(ChewingSensorProtocol.TryParse("CAL_DONE,4294967295", out var done), Is.True);
            Assert.That(done.RequestId, Is.EqualTo(uint.MaxValue));

            // フェーズ順序違反はエラー理由として返る (仕様書 §9.3)。
            Assert.That(ChewingSensorProtocol.TryParse("CAL_FAILED,42,NOT_STARTED", out var notStarted), Is.True);
            Assert.That(notStarted.FailureReason, Is.EqualTo("NOT_STARTED"));

            Assert.That(ChewingSensorProtocol.TryParse("CAL_FAILED,42,ABORTED", out var aborted), Is.True);
            Assert.That(aborted.FailureReason, Is.EqualTo("ABORTED"));

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
            foreach (var line in new List<string>
                     {
                         null, "", "PING", "MOUTH", "MOUTH,OPEN,EXTRA", "CAL_FAILED,43",
                         "CAL_NOISE_DONE", "CAL_CHEW_DONE,0", "cal_noise_done,42"
                     })
            {
                Assert.That(ChewingSensorProtocol.TryParse(line, out _), Is.False, $"入力: {line}");
            }
        }
    }
}
