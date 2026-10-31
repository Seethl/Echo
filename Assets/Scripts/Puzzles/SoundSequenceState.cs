using System;

namespace Echo.Puzzles
{
    /// <summary>Ordered listening puzzle rules, independent of audio, scene objects and UI.</summary>
    public sealed class SoundSequenceState
    {
        public enum Result { Rejected, Correct, Mistake, Solved }
        private readonly int[] pattern;
        private readonly int toneCount;
        public int Position { get; private set; }
        public int Length => pattern.Length;
        public bool IsSolved { get; private set; }

        public SoundSequenceState(int[] sequence, int availableTones)
        {
            if (sequence == null || sequence.Length == 0 || availableTones < 1)
                throw new ArgumentException("A puzzle requires a non-empty sequence and available tones.");
            pattern = (int[])sequence.Clone();
            toneCount = availableTones;
            foreach (int tone in pattern)
                if (tone < 0 || tone >= toneCount) throw new ArgumentException("Sequence refers to an unavailable tone.");
        }

        public int ToneAt(int index) => pattern[index];

        public Result Submit(int tone)
        {
            if (IsSolved || tone < 0 || tone >= toneCount) return Result.Rejected;
            if (pattern[Position] != tone)
            {
                Position = 0;
                return Result.Mistake;
            }
            Position++;
            if (Position < pattern.Length) return Result.Correct;
            IsSolved = true;
            return Result.Solved;
        }

        public void ResetAttempt() { if (!IsSolved) Position = 0; }
    }
}
