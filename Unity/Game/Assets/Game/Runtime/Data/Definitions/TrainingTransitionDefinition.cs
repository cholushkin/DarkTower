using Alchemy.Inspector;
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

        [Button]
        private void PrintInfo()
        {
            Debug.Log($"[TrainingTransition] {fromRank}->{toRank} isAdjacent={IsAdjacent} " +
                $"cost(gold={cost.Gold},crystals={cost.Crystals}) duration={duration.Value}{duration.Unit}", this);
        }
    }
}
