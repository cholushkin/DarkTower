using System;

namespace Game
{
    /// Seedable randomness seam for every gameplay roll (raid outcome/injury, dungeon processing
    /// outcome, reward draws, recruitment injury). Never use UnityEngine.Random for these so tests
    /// can reproduce a run exactly from a seed.
    public interface IRandomSource
    {
        /// Uniform value in [0, 1).
        float NextFloat01();

        /// Uniform integer in [minInclusive, maxInclusive].
        int NextInt(int minInclusive, int maxInclusive);

        /// True with the given probability (clamped to [0, 1]).
        bool NextBool(float probability);
    }

    /// Default IRandomSource backed by a seeded System.Random.
    public sealed class SystemRandomSource : IRandomSource
    {
        private readonly Random _random;

        public SystemRandomSource(int seed) => _random = new Random(seed);

        public float NextFloat01() => (float)_random.NextDouble();

        public int NextInt(int minInclusive, int maxInclusive)
        {
            if (maxInclusive < minInclusive) maxInclusive = minInclusive;
            return _random.Next(minInclusive, maxInclusive + 1);
        }

        public bool NextBool(float probability)
        {
            if (probability <= 0f) return false;
            if (probability >= 1f) return true;
            return NextFloat01() < probability;
        }
    }
}
