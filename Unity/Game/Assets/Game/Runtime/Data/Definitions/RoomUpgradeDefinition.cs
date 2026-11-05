using Alchemy.Inspector;
using UnityEngine;

namespace Game
{
    /// One individually purchased room upgrade. A paid upgrade starts a timed construction; its
    /// effect activates on completion. Sequential chains are expressed via series id + step index
    /// and prerequisite upgrades.
    [CreateAssetMenu(menuName = "Dark Tower/Room Upgrade", fileName = "RoomUpgrade")]
    public sealed class RoomUpgradeDefinition : ScriptableObject, IIdentifiedDefinition
    {
        [SerializeField] private string upgradeId;
        [SerializeField] private RoomId roomId;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;

        [Header("Sequence (optional)")]
        [SerializeField] private string seriesId;
        [SerializeField, Min(0)] private int stepIndex;

        [Header("Requirements")]
        [SerializeField] private ResourceCost cost;
        [SerializeField] private GameDuration duration;
        [SerializeField] private RoomUpgradeDefinition[] prerequisiteUpgrades;
        [SerializeField] private UnlockCondition[] additionalRequirements;

        [Header("Result")]
        [SerializeField] private UpgradeEffect[] effects;
        [SerializeField, Min(0)] private long towerXpReward;

        public string Id => upgradeId;
        public RoomId RoomId => roomId;
        public string DisplayName => displayName;
        public string Description => description;

        public string SeriesId => seriesId;
        public int StepIndex => stepIndex;

        public ResourceCost Cost => cost;
        public GameDuration Duration => duration;
        public RoomUpgradeDefinition[] PrerequisiteUpgrades => prerequisiteUpgrades;
        public UnlockCondition[] AdditionalRequirements => additionalRequirements;

        public UpgradeEffect[] Effects => effects;
        public long TowerXpReward => towerXpReward;

        public bool IsSequential => !string.IsNullOrEmpty(seriesId);

        [Button]
        private void PrintInfo()
        {
            Debug.Log($"[RoomUpgrade:{upgradeId}] '{displayName}' room={roomId} isSequential={IsSequential} " +
                $"seriesId='{seriesId}' stepIndex={stepIndex} cost(gold={cost.Gold},crystals={cost.Crystals}) " +
                $"towerXpReward={towerXpReward}", this);
        }
    }
}
