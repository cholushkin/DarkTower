using UnityEngine;

namespace Game
{
    /// One allowed rank transition in the Training Hall (1->2 .. 4->5), with its own cost and
    /// duration, unlocked by an advanced-training upgrade.
    [CreateAssetMenu(menuName = "Dark Tower/Training Transition", fileName = "TrainingTransition")]
    public sealed class TrainingTransitionDefinition : ScriptableObject
    {
        [SerializeField, Range(1, 5)] private int fromRank = 1;
        [SerializeField, Range(1, 5)] private int toRank = 2;
        [SerializeField] private ResourceCost cost;
        [SerializeField] private GameDuration duration;
        [SerializeField] private RoomUpgradeDefinition requiredAdvancedTrainingUpgrade;

        public int FromRank => fromRank;
        public int ToRank => toRank;
        public ResourceCost Cost => cost;
        public GameDuration Duration => duration;
        public RoomUpgradeDefinition RequiredAdvancedTrainingUpgrade => requiredAdvancedTrainingUpgrade;

        public bool IsAdjacent => toRank == fromRank + 1;
    }

    /// A Laboratory research project: a uniquely authored permanent global improvement.
    /// Only one research can be active at a time; each completes once.
    [CreateAssetMenu(menuName = "Dark Tower/Research", fileName = "Research")]
    public sealed class ResearchDefinition : ScriptableObject, IIdentifiedDefinition
    {
        [SerializeField] private string researchId;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField, Min(0)] private int researchTier;
        [SerializeField] private ResearchPrerequisites prerequisites;
        [SerializeField] private ResourceCost cost;
        [SerializeField] private GameDuration duration;
        [SerializeField] private GlobalEffect[] effects;

        public string Id => researchId;
        public string DisplayName => displayName;
        public string Description => description;
        public int ResearchTier => researchTier;
        public ResearchPrerequisites Prerequisites => prerequisites;
        public ResourceCost Cost => cost;
        public GameDuration Duration => duration;
        public GlobalEffect[] Effects => effects;
    }
}
