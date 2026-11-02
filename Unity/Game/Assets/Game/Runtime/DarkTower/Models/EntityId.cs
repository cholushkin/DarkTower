using System;
using System.Threading;

namespace Game
{
    /// Stable identity for a runtime entity (minion, item stack, prisoner, raid, activity).
    /// Independent of list position and of Unity object instance ids.
    [Serializable]
    public readonly struct EntityId : IEquatable<EntityId>
    {
        public readonly long Value;

        public EntityId(long value) => Value = value;

        public bool IsValid => Value != 0;
        public bool Equals(EntityId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is EntityId other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString();

        public static readonly EntityId None = new EntityId(0);
    }

    /// Monotonic allocator owned by a session. Hands out unique, never-reused ids within a run.
    public sealed class EntityIdAllocator
    {
        private long _next;

        public EntityIdAllocator(long seed = 0) => _next = seed;

        public EntityId Next() => new EntityId(Interlocked.Increment(ref _next));
    }
}
