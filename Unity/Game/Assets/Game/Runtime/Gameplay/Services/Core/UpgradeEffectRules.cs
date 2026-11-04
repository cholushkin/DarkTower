namespace Game
{
    /// Aggregates completed-upgrade UpgradeEffects by kind. UpgradeEffect.Amount's convention is an
    /// explicitly open balance decision (flat vs factor; see UpgradeEffect's doc comment); this
    /// gameplay layer resolves it as follows, documented here rather than invented silently per call
    /// site:
    ///  - Reduce* duration kinds: Amount is a MULTIPLICATIVE factor applied per completed step, in
    ///    completion order (e.g. two steps of 0.9 compound to 0.81) — a sequence of "sale/treatment/
    ///    research/torture efficiency" steps reads naturally as successive percentage reductions, and
    ///    multiplying keeps durations from going to zero or negative regardless of step count.
    ///  - IncreaseRecruitmentChance: Amount is an ADDITIVE probability delta, summed across completed
    ///    steps, consistent with "increases recruitment chance" reading as a flat percentage-point
    ///    bump per step.
    public static class UpgradeEffectRules
    {
        public static float GetDurationFactor(TowerModel tower, DarkTowerDatabase db, UpgradeEffectKind kind)
        {
            float factor = 1f;
            if (tower == null || db == null) return factor;
            var ids = tower.CompletedUpgradeIds;
            for (int i = 0; i < ids.Count; i++)
            {
                var def = db.GetUpgrade(ids[i]);
                if (def?.Effects == null) continue;
                for (int e = 0; e < def.Effects.Length; e++)
                    if (def.Effects[e].Kind == kind) factor *= def.Effects[e].Amount;
            }
            return factor;
        }

        public static float GetAdditiveAmount(TowerModel tower, DarkTowerDatabase db, UpgradeEffectKind kind)
        {
            float total = 0f;
            if (tower == null || db == null) return total;
            var ids = tower.CompletedUpgradeIds;
            for (int i = 0; i < ids.Count; i++)
            {
                var def = db.GetUpgrade(ids[i]);
                if (def?.Effects == null) continue;
                for (int e = 0; e < def.Effects.Length; e++)
                    if (def.Effects[e].Kind == kind) total += def.Effects[e].Amount;
            }
            return total;
        }

        /// Counts completed-upgrade effects of a kind that is itself a step counter rather than a
        /// magnitude (e.g. UnlockResearchTier: "how many tiers has the player unlocked beyond the
        /// starting one", not a reduction factor or additive bonus).
        public static int CountCompletedEffectsOfKind(TowerModel tower, DarkTowerDatabase db, UpgradeEffectKind kind)
        {
            int count = 0;
            if (tower == null || db == null) return count;
            var ids = tower.CompletedUpgradeIds;
            for (int i = 0; i < ids.Count; i++)
            {
                var def = db.GetUpgrade(ids[i]);
                if (def?.Effects == null) continue;
                for (int e = 0; e < def.Effects.Length; e++)
                    if (def.Effects[e].Kind == kind) count++;
            }
            return count;
        }
    }
}
