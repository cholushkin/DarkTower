using Alchemy.Inspector;
using UnityEngine;

namespace Game
{
    /// Optional shared reward distribution referenced by multiple missions. Mission-local reward
    /// data (MissionDefinition.ItemRewards) is equally valid; use this only to share a pool.
    [CreateAssetMenu(menuName = "Dark Tower/Reward Table", fileName = "RewardTable")]
    public sealed class RewardTableDefinition : ScriptableObject, IIdentifiedDefinition
    {
        [SerializeField] private string rewardTableId;
        [SerializeField] private IntRange goldRange;
        [SerializeField] private IntRange crystalRange;
        [SerializeField] private ItemRewardEntry[] itemRewards;

        public string Id => rewardTableId;
        public IntRange GoldRange => goldRange;
        public IntRange CrystalRange => crystalRange;
        public ItemRewardEntry[] ItemRewards => itemRewards;

        [Button]
        private void PrintInfo()
        {
            Debug.Log($"[RewardTable:{rewardTableId}] gold={goldRange.Min}-{goldRange.Max} " +
                $"crystals={crystalRange.Min}-{crystalRange.Max} itemRewards={itemRewards?.Length ?? 0}", this);
        }
    }
}
