using System;
using UnityEngine;

namespace Game
{
    /// A designer-authored duration. Stored with an explicit unit; converted to game-hours
    /// against the clock's configured hours-per-day (GlobalBalanceDefinition).
    [Serializable]
    public struct GameDuration
    {
        [SerializeField, Min(0f)] private float value;
        [SerializeField] private DurationUnit unit;

        public GameDuration(float value, DurationUnit unit)
        {
            this.value = value;
            this.unit = unit;
        }

        public float Value => value;
        public DurationUnit Unit => unit;

        public float ToHours(float gameHoursPerDay)
        {
            return unit == DurationUnit.Days ? value * gameHoursPerDay : value;
        }

        public float ToDays(float gameHoursPerDay)
        {
            if (gameHoursPerDay <= 0f) return 0f;
            return unit == DurationUnit.Days ? value : value / gameHoursPerDay;
        }
    }

    /// Inclusive integer range used for currency drops, prisoner counts, and reward draw counts.
    [Serializable]
    public struct IntRange
    {
        [SerializeField] private int min;
        [SerializeField] private int max;

        public IntRange(int min, int max)
        {
            this.min = min;
            this.max = max;
        }

        public int Min => min;
        public int Max => max;

        public bool IsValid => min <= max && min >= 0;
        public int Clamp(int v) => v < min ? min : (v > max ? max : v);
    }

    /// A probability in the closed range [0, 1].
    [Serializable]
    public struct Probability
    {
        [SerializeField, Range(0f, 1f)] private float value;

        public Probability(float value)
        {
            this.value = Mathf.Clamp01(value);
        }

        public float Value => value;
        public bool IsValid => value >= 0f && value <= 1f;
    }
}
