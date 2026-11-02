using UnityEngine;

namespace Game
{
    /// One individually selectable village/location on the World Map. Reward generation is
    /// independent of squad size; the squad affects combat and carry, not the authored reward pool.
    [CreateAssetMenu(menuName = "Dark Tower/Mission", fileName = "Mission")]
    public sealed class MissionDefinition : ScriptableObject, IIdentifiedDefinition
    {
        [SerializeField] private string missionId;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private Sprite icon;

        [Header("Challenge")]
        [SerializeField, Min(0f)] private float requiredPower;
        [SerializeField] private GameDuration baseDuration;
        [SerializeField] private Probability baseInjuryChance;

        [Header("Discoveries")]
        [SerializeField] private IntRange goldRange;
        [SerializeField] private IntRange crystalRange;
        [SerializeField] private ItemRewardEntry[] itemRewards;
        [SerializeField] private IntRange prisonerCountRange;
        [SerializeField] private RankWeight[] prisonerRankDistribution;

        [Header("Access")]
        [SerializeField] private UnlockCondition[] unlockConditions;
        [SerializeField] private SquadConstraints squadConstraints;

        public string Id => missionId;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;

        public float RequiredPower => requiredPower;
        public GameDuration BaseDuration => baseDuration;
        public Probability BaseInjuryChance => baseInjuryChance;

        public IntRange GoldRange => goldRange;
        public IntRange CrystalRange => crystalRange;
        public ItemRewardEntry[] ItemRewards => itemRewards;
        public IntRange PrisonerCountRange => prisonerCountRange;
        public RankWeight[] PrisonerRankDistribution => prisonerRankDistribution;

        public UnlockCondition[] UnlockConditions => unlockConditions;
        public SquadConstraints SquadConstraints => squadConstraints;
    }
}
