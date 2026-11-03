namespace Game
{
    /// Applies one completed room upgrade's gameplay Effects[] to session capacities/unlocks. Used by
    /// both TowerUpgradeService (a purchase's construction completing) and SessionStateFactory (a
    /// StartingCompletedUpgrades entry, which otherwise only records completion ownership and never
    /// runs through ProcessDue) so a capacity/unlock effect is applied through exactly one shared path
    /// regardless of when the upgrade became completed, and never double-applied.
    ///
    /// Deliberately excludes RoomUpgradeDefinition.TowerXpReward: that is a reward for completing a
    /// purchase just now, not a derived session-state effect, so a starting completed upgrade never
    /// re-grants it — StartingBalanceDefinition.StartingTowerXp is the sole authority for a new
    /// session's starting XP.
    public static class UpgradeEffectApplier
    {
        public static void Apply(GameSessionModel session, RoomUpgradeDefinition def)
        {
            var effects = def != null ? def.Effects : null;
            if (session == null || effects == null) return;

            for (int i = 0; i < effects.Length; i++)
            {
                var effect = effects[i];
                switch (effect.Kind)
                {
                    case UpgradeEffectKind.AddStorageSlots:
                        session.Inventory.AddSlotCapacity((int)effect.Amount);
                        break;
                    case UpgradeEffectKind.AddQuartersBed:
                        session.Recovery.AddQuartersBeds((int)effect.Amount);
                        break;
                    case UpgradeEffectKind.AddInfirmaryBed:
                        session.Recovery.AddInfirmaryBeds((int)effect.Amount);
                        break;
                    case UpgradeEffectKind.AddTrainingStation:
                        session.Training.AddStations((int)effect.Amount);
                        break;
                    case UpgradeEffectKind.AddDungeonCell:
                        session.Dungeon.AddCellCapacity((int)effect.Amount);
                        break;
                    case UpgradeEffectKind.UnlockTrainingTransition:
                        UnlockTransition(session, effect.TargetId);
                        break;
                    case UpgradeEffectKind.AdvanceEnemyIntelligence:
                        session.Intelligence.AdvanceEnemyStage();
                        break;
                    case UpgradeEffectKind.AdvanceLootIntelligence:
                        session.Intelligence.AdvanceLootStage();
                        break;
                    // UnlockResearchTier is read on demand (ResearchModel.BaseUnlockedResearchTier +
                    // completed-upgrade count, see ResearchService) rather than stored here.
                    // ReduceSaleDuration/ReduceTreatmentDuration/ReduceResearchDuration/
                    // ReduceTortureDuration/IncreaseRecruitmentChance are likewise read on demand via
                    // UpgradeEffectRules against TowerModel.CompletedUpgradeIds, which the caller
                    // (TowerUpgradeService or SessionStateFactory) already updated before calling here.
                    // Custom is a designer escape hatch with no mechanics defined yet.
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

        private static void UnlockTransition(GameSessionModel session, string targetId)
        {
            if (string.IsNullOrEmpty(targetId)) return;
            var parts = targetId.Split('-');
            if (parts.Length != 2) return;
            if (int.TryParse(parts[0], out var from) && int.TryParse(parts[1], out var to))
                session.Training.UnlockTransition(from, to);
        }
    }
}
