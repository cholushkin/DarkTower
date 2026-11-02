using UnityEngine;

namespace Game
{
    /// One of the seven rooms: its base capabilities and the upgrades it offers.
    /// Rooms do not have levels; progression is tracked through completed upgrade ids.
    [CreateAssetMenu(menuName = "Dark Tower/Room", fileName = "Room")]
    public sealed class RoomDefinition : ScriptableObject
    {
        [SerializeField] private RoomId roomId;
        [SerializeField] private string displayName;
        [SerializeField] private RoomCapabilities startingCapabilities;
        [SerializeField] private RoomUpgradeDefinition[] upgrades;

        public RoomId RoomId => roomId;
        public string DisplayName => displayName;
        public RoomCapabilities StartingCapabilities => startingCapabilities;
        public RoomUpgradeDefinition[] Upgrades => upgrades;
    }

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
    }
}
