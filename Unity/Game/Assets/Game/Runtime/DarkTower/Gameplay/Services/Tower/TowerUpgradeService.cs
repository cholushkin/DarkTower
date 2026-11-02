namespace Game
{
    /// Room upgrade purchases and their timed construction. A purchased upgrade's gameplay effect
    /// activates only on completion, never at purchase time (per the design's explicit "becomes
    /// active when completed" rule). Tower XP is likewise awarded on completion, resolving the
    /// catalog's open "purchase vs completion" question in favor of the only point the design
    /// describes as observable ("its gameplay effect becomes active when completed").
    ///
    /// Concurrency (open design question): any number of upgrades may be under construction at once
    /// — nothing in the design or catalog caps concurrent construction, so no artificial limit is
    /// imposed; this is a documented default, not an invented balance rule.
    public sealed class TowerUpgradeService
    {
        private readonly GameSessionModel _session;
        private readonly EconomyService _economy;
        private readonly ClockService _clock;

        public TowerUpgradeService(GameSessionModel session, EconomyService economy, ClockService clock)
        {
            _session = session;
            _economy = economy;
            _clock = clock;
        }

        private TowerModel Tower => _session.Tower;
        private DarkTowerDatabase Db => _session.Database;

        public OperationResult<EntityId> TryPurchaseUpgrade(string upgradeId)
        {
            if (_session.IsTerminal) return OperationResult<EntityId>.Fail("game_over");

            var def = Db.GetUpgrade(upgradeId);
            if (def == null) return OperationResult<EntityId>.Fail("upgrade_not_found");
            if (Tower.IsUpgradeCompleted(upgradeId)) return OperationResult<EntityId>.Fail("already_completed");
            if (IsInProgress(upgradeId)) return OperationResult<EntityId>.Fail("already_in_progress");

            var prereqs = def.PrerequisiteUpgrades;
            if (prereqs != null)
            {
                for (int i = 0; i < prereqs.Length; i++)
                {
                    if (prereqs[i] != null && !Tower.IsUpgradeCompleted(prereqs[i].Id))
                        return OperationResult<EntityId>.Fail("prerequisite_not_met");
                }
            }

            if (!UnlockConditionRules.AllSatisfied(def.AdditionalRequirements, _session))
                return OperationResult<EntityId>.Fail("requirement_not_met");

            if (!_economy.CanAfford(def.Cost)) return OperationResult<EntityId>.Fail("insufficient_resources");
            var spend = _economy.TrySpend(def.Cost);
            if (!spend.Success) return OperationResult<EntityId>.Fail(spend.Reason);

            float now = _clock.NowHours;
            float duration = def.Duration.ToHours(_clock.GameHoursPerDay);
            var id = _session.IdAllocator.Next();
            var process = new UpgradeProcessModel(id, upgradeId, now, now + duration);
            Tower.AddInProgress(process);

            return OperationResult<EntityId>.Ok(id);
        }

        public void ProcessDue(float nowHours)
        {
            var list = Tower.InProgressUpgrades;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var process = list[i];
                if (process.DueGameHours > nowHours) continue;

                process.Complete();
                ApplyEffects(process.UpgradeId);
                Tower.MarkUpgradeCompleted(process.UpgradeId);

                var def = Db.GetUpgrade(process.UpgradeId);
                if (def != null && def.TowerXpReward > 0) Tower.AddXp(def.TowerXpReward);

                Tower.RemoveInProgress(process);
            }
        }

        public float? NextDueHours()
        {
            float? min = null;
            var list = Tower.InProgressUpgrades;
            for (int i = 0; i < list.Count; i++)
                if (min == null || list[i].DueGameHours < min.Value) min = list[i].DueGameHours;
            return min;
        }

        private bool IsInProgress(string upgradeId)
        {
            var list = Tower.InProgressUpgrades;
            for (int i = 0; i < list.Count; i++)
                if (list[i].UpgradeId == upgradeId) return true;
            return false;
        }

        private void ApplyEffects(string upgradeId)
        {
            var def = Db.GetUpgrade(upgradeId);
            var effects = def != null ? def.Effects : null;
            if (effects == null) return;

            for (int i = 0; i < effects.Length; i++)
            {
                var effect = effects[i];
                switch (effect.Kind)
                {
                    case UpgradeEffectKind.AddStorageSlots:
                        _session.Inventory.AddSlotCapacity((int)effect.Amount);
                        break;
                    case UpgradeEffectKind.AddQuartersBed:
                        _session.Recovery.AddQuartersBeds((int)effect.Amount);
                        break;
                    case UpgradeEffectKind.AddInfirmaryBed:
                        _session.Recovery.AddInfirmaryBeds((int)effect.Amount);
                        break;
                    case UpgradeEffectKind.AddTrainingStation:
                        _session.Training.AddStations((int)effect.Amount);
                        break;
                    case UpgradeEffectKind.AddDungeonCell:
                        _session.Dungeon.AddCellCapacity((int)effect.Amount);
                        break;
                    case UpgradeEffectKind.UnlockTrainingTransition:
                        UnlockTransition(effect.TargetId);
                        break;
                    case UpgradeEffectKind.AdvanceEnemyIntelligence:
                        _session.Intelligence.AdvanceEnemyStage();
                        break;
                    case UpgradeEffectKind.AdvanceLootIntelligence:
                        _session.Intelligence.AdvanceLootStage();
                        break;
                    // UnlockResearchTier is read on demand (starting tier + completed-upgrade count,
                    // see ResearchService) rather than stored. ReduceSaleDuration/ReduceTreatmentDuration/
                    // ReduceResearchDuration/ReduceTortureDuration/IncreaseRecruitmentChance are also
                    // read on demand via UpgradeEffectRules against CompletedUpgradeIds, which
                    // MarkUpgradeCompleted (above) already updated. Custom is a designer escape hatch
                    // with no mechanics defined yet.
                    case UpgradeEffectKind.UnlockResearchTier:
                    case UpgradeEffectKind.ReduceSaleDuration:
                    case UpgradeEffectKind.ReduceTreatmentDuration:
                    case UpgradeEffectKind.ReduceResearchDuration:
                    case UpgradeEffectKind.ReduceTortureDuration:
                    case UpgradeEffectKind.IncreaseRecruitmentChance:
                    case UpgradeEffectKind.Custom:
                    default:
                        break;
                }
            }
        }

        private void UnlockTransition(string targetId)
        {
            if (string.IsNullOrEmpty(targetId)) return;
            var parts = targetId.Split('-');
            if (parts.Length != 2) return;
            if (int.TryParse(parts[0], out var from) && int.TryParse(parts[1], out var to))
                _session.Training.UnlockTransition(from, to);
        }
    }
}
