using UnityEngine;

namespace Game
{
    /// Estimated enemy strength shown to the player for one mission at the currently achieved War
    /// Room enemy-intelligence stage. HasEstimate false means fully hidden.
    public readonly struct EnemyIntelligenceView
    {
        public readonly bool HasEstimate;
        public readonly float EstimateMin;
        public readonly float EstimateMax;
        public EnemyIntelligenceView(bool hasEstimate, float min, float max)
        {
            HasEstimate = hasEstimate; EstimateMin = min; EstimateMax = max;
        }
    }

    /// Loot visibility at the currently achieved War Room loot-intelligence stage.
    public readonly struct LootIntelligenceView
    {
        public readonly bool RevealItemTypes;
        public readonly bool RevealDropEstimates;
        public readonly float EstimatePrecision;
        public LootIntelligenceView(bool revealItemTypes, bool revealDropEstimates, float estimatePrecision)
        {
            RevealItemTypes = revealItemTypes; RevealDropEstimates = revealDropEstimates; EstimatePrecision = estimatePrecision;
        }
    }

    /// Query-only War Room intelligence: turns achieved stages + mission data into a display view,
    /// never touching the mission's actual required power, difficulty or reward generation.
    ///
    /// Enemy estimate range (documented formula, not an authored balance value): at precision p in
    /// [0,1] the shown range is centered on the true required power with width = requiredPower*(1-p),
    /// collapsing to the exact value at p=1 and fully hidden at p=0 — a direct reading of
    /// "progressively tighter estimated ranges" using only the authored precision curve.
    public sealed class IntelligenceService
    {
        private readonly GameSessionModel _session;

        public IntelligenceService(GameSessionModel session)
        {
            _session = session;
        }

        private WarRoomIntelligenceDefinition Rules => _session.Database.WarRoomIntelligence;

        public EnemyIntelligenceView GetEnemyIntelligence(MissionDefinition mission)
        {
            if (mission == null || Rules == null) return default;
            var stages = Rules.EnemyIntelligenceStages;
            var best = BestStage(stages, _session.Intelligence.EnemyStage.CurrentValue, out bool found);
            if (!found || best.EstimatePrecision <= 0f) return new EnemyIntelligenceView(false, 0f, 0f);

            float width = mission.RequiredPower * (1f - best.EstimatePrecision);
            float min = Mathf.Max(0f, mission.RequiredPower - width / 2f);
            float max = mission.RequiredPower + width / 2f;
            return new EnemyIntelligenceView(true, min, max);
        }

        public LootIntelligenceView GetLootIntelligence()
        {
            if (Rules == null) return default;
            var stages = Rules.LootIntelligenceStages;
            var best = BestLootStage(stages, _session.Intelligence.LootStage.CurrentValue, out bool found);
            if (!found) return new LootIntelligenceView(false, false, 0f);
            return new LootIntelligenceView(best.RevealItemTypes, best.RevealDropEstimates, best.EstimatePrecision);
        }

        private static EnemyIntelStage BestStage(EnemyIntelStage[] stages, int achieved, out bool found)
        {
            found = false;
            EnemyIntelStage best = default;
            int bestStage = -1;
            if (stages == null) return best;
            for (int i = 0; i < stages.Length; i++)
            {
                if (stages[i].Stage <= achieved && stages[i].Stage > bestStage)
                {
                    best = stages[i];
                    bestStage = stages[i].Stage;
                    found = true;
                }
            }
            return best;
        }

        private static LootIntelStage BestLootStage(LootIntelStage[] stages, int achieved, out bool found)
        {
            found = false;
            LootIntelStage best = default;
            int bestStage = -1;
            if (stages == null) return best;
            for (int i = 0; i < stages.Length; i++)
            {
                if (stages[i].Stage <= achieved && stages[i].Stage > bestStage)
                {
                    best = stages[i];
                    bestStage = stages[i].Stage;
                    found = true;
                }
            }
            return best;
        }
    }
}
