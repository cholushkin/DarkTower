using System.Collections.Generic;

namespace Game
{
    /// Optional helper for a caller that wants a default carry-budget selection instead of supplying
    /// its own (e.g. a future UI that pre-ticks items before the player adjusts them). The design
    /// explicitly leaves the default-preselection policy open ("allow the caller to supply a legal
    /// selection or expose an independently configurable selection policy") — RaidService itself never
    /// picks favorites; this is a separate, swappable pure function a caller may use or ignore.
    /// Default policy: greedy, in discovery order, until the remaining carry budget is exhausted.
    public static class RaidClaimPolicy
    {
        public static IReadOnlyList<EntityId> DefaultPreselect(IReadOnlyList<DiscoveredItemCopy> copies, int carryLimit)
        {
            var selected = new List<EntityId>();
            if (copies == null) return selected;

            int remaining = carryLimit;
            for (int i = 0; i < copies.Count; i++)
            {
                var copy = copies[i];
                if (copy.CarrySize <= remaining)
                {
                    selected.Add(copy.Id);
                    remaining -= copy.CarrySize;
                }
            }
            return selected;
        }
    }
}
