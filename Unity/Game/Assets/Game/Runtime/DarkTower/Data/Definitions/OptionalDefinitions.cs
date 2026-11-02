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
    }

    /// Optional event/secret definition. Dungeon interrogation can reveal a secret that unlocks map
    /// locations or sets world flags. Effects here are data references resolved by future systems.
    [CreateAssetMenu(menuName = "Dark Tower/Event", fileName = "Event")]
    public sealed class EventDefinition : ScriptableObject, IIdentifiedDefinition
    {
        [SerializeField] private string eventId;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private string setsWorldFlag;
        [SerializeField] private MissionDefinition[] unlocksMissions;

        public string Id => eventId;
        public string DisplayName => displayName;
        public string Description => description;
        public string SetsWorldFlag => setsWorldFlag;
        public MissionDefinition[] UnlocksMissions => unlocksMissions;
    }
}
