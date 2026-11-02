namespace Game
{
    /// Laboratory research: at most one active process, no queue, each project completes once with
    /// permanent global effects (read on demand from completed ids via ResearchModel/StatCalculator —
    /// nothing here duplicates that aggregation). The currently unlocked research tier is the
    /// authored starting tier plus one per completed research_tier_unlock upgrade; it is computed on
    /// demand rather than stored, since StartingBalanceDefinition and TowerModel.CompletedUpgradeIds
    /// already carry everything needed.
    public sealed class ResearchService
    {
        private readonly GameSessionModel _session;
        private readonly EconomyService _economy;
        private readonly ClockService _clock;

        public ResearchService(GameSessionModel session, EconomyService economy, ClockService clock)
        {
            _session = session;
            _economy = economy;
            _clock = clock;
        }

        private ResearchModel Research => _session.Research;
        private DarkTowerDatabase Db => _session.Database;

        public int CurrentUnlockedResearchTier()
        {
            int baseTier = Db.StartingBalance != null ? Db.StartingBalance.InitialRoomCapabilities.UnlockedResearchTier : 0;
            return baseTier + UpgradeEffectRules.CountCompletedEffectsOfKind(_session.Tower, Db, UpgradeEffectKind.UnlockResearchTier);
        }

        public OperationResult<EntityId> TryStartResearch(string researchId)
        {
            if (_session.IsTerminal) return OperationResult<EntityId>.Fail("game_over");

            var def = Db.GetResearch(researchId);
            if (def == null) return OperationResult<EntityId>.Fail("research_not_found");
            if (Research.IsCompleted(researchId)) return OperationResult<EntityId>.Fail("already_completed");
            if (Research.HasActiveProcess) return OperationResult<EntityId>.Fail("research_in_progress");
            if (def.ResearchTier > CurrentUnlockedResearchTier()) return OperationResult<EntityId>.Fail("tier_locked");

            var prereqs = def.Prerequisites;
            var requiredResearch = prereqs.RequiredResearch;
            if (requiredResearch != null)
            {
                for (int i = 0; i < requiredResearch.Length; i++)
                {
                    if (requiredResearch[i] != null && !Research.IsCompleted(requiredResearch[i].Id))
                        return OperationResult<EntityId>.Fail("prerequisite_not_met");
                }
            }

            var requiredUpgrades = prereqs.RequiredUpgrades;
            if (requiredUpgrades != null)
            {
                for (int i = 0; i < requiredUpgrades.Length; i++)
                {
                    if (requiredUpgrades[i] != null && !_session.Tower.IsUpgradeCompleted(requiredUpgrades[i].Id))
                        return OperationResult<EntityId>.Fail("prerequisite_not_met");
                }
            }

            if (!UnlockConditionRules.AllSatisfied(prereqs.Additional, _session))
                return OperationResult<EntityId>.Fail("requirement_not_met");

            if (!_economy.CanAfford(def.Cost)) return OperationResult<EntityId>.Fail("insufficient_resources");
            var spend = _economy.TrySpend(def.Cost);
            if (!spend.Success) return OperationResult<EntityId>.Fail(spend.Reason);

            float now = _clock.NowHours;
            float factor = UpgradeEffectRules.GetDurationFactor(_session.Tower, Db, UpgradeEffectKind.ReduceResearchDuration);
            float duration = UnityEngine.Mathf.Max(0f, def.Duration.ToHours(_clock.GameHoursPerDay) * factor);
            var id = _session.IdAllocator.Next();
            var process = new ResearchProcessModel(id, researchId, now, now + duration);
            Research.SetActiveProcess(process);

            return OperationResult<EntityId>.Ok(id);
        }

        public void ProcessDue(float nowHours)
        {
            var process = Research.ActiveProcess.CurrentValue;
            if (process == null) return;
            if (process.DueGameHours > nowHours) return;

            process.Complete();
            Research.MarkCompleted(process.ResearchId);
            Research.ClearActiveProcess();
        }

        public float? NextDueHours()
        {
            var process = Research.ActiveProcess.CurrentValue;
            return process != null ? process.DueGameHours : (float?)null;
        }
    }
}
