namespace Game
{
    /// Evaluates an authored UnlockCondition against current session state. Shared by
    /// TowerUpgradeService (additionalRequirements), ResearchService (prerequisites.Additional) and
    /// WorldProgressionService (mission unlockConditions) so the six condition kinds are interpreted
    /// identically everywhere instead of being re-implemented per call site.
    public static class UnlockConditionRules
    {
        public static bool AllSatisfied(UnlockCondition[] conditions, GameSessionModel session)
        {
            if (conditions == null) return true;
            for (int i = 0; i < conditions.Length; i++)
                if (!IsSatisfied(conditions[i], session)) return false;
            return true;
        }

        public static bool IsSatisfied(UnlockCondition condition, GameSessionModel session)
        {
            switch (condition.Type)
            {
                case UnlockConditionType.TierCompleted:
                    return IsTierCompleted(condition.TargetId, session);
                case UnlockConditionType.MissionCompletedOnce:
                    var mission = session.World.FindMission(condition.TargetId);
                    return mission != null && mission.CompletedOnce.CurrentValue;
                case UnlockConditionType.UpgradeCompleted:
                    return session.Tower.IsUpgradeCompleted(condition.TargetId);
                case UnlockConditionType.ResearchCompleted:
                    return session.Research.IsCompleted(condition.TargetId);
                case UnlockConditionType.SecretRevealed:
                case UnlockConditionType.EventFlag:
                    return session.World.HasFlag(condition.TargetId);
                default:
                    return false;
            }
        }

        private static bool IsTierCompleted(string tierId, GameSessionModel session)
        {
            var worldMap = session.Database.WorldMap;
            var tiers = worldMap != null ? worldMap.Tiers : null;
            if (tiers == null) return false;

            for (int i = 0; i < tiers.Length; i++)
            {
                if (tiers[i].TierId != tierId) continue;
                var missions = tiers[i].Missions;
                if (missions == null) return true;
                for (int j = 0; j < missions.Length; j++)
                {
                    var missionDef = missions[j].Mission;
                    if (missionDef == null) continue;
                    var progress = session.World.FindMission(missionDef.Id);
                    if (progress == null || !progress.CompletedOnce.CurrentValue) return false;
                }
                return true;
            }
            return false;
        }
    }
}
