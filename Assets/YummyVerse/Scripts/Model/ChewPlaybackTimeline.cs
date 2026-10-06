using System;
namespace YummyVerse.Scripts.Model
{
    public sealed class ChewPlaybackTimeline
    {
        public const double MaximumDuration = 0.8;
        public bool IsPlaying { get; private set; }
        public double Cursor { get; private set; }
        public double Deadline => _startedAt + MaximumDuration;
        private double _startedAt;
        private double _length;
        public void Reset(double length) { _length = length; Cursor = 0; IsPlaying = false; }
        public bool Close(double now)
        {
            if (IsPlaying || _length <= 0) return false;
            _startedAt = now; IsPlaying = true; return true;
        }
        public void Open(double now)
        {
            if (!IsPlaying) return;
            Cursor = (Cursor + Math.Max(0, Math.Min(MaximumDuration, now - _startedAt))) % _length;
            IsPlaying = false;
        }
    }
}
