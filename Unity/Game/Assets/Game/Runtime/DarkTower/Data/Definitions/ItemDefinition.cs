using UnityEngine;

namespace Game
{
    /// A single physical item type (weapon, material, magic item, or treasure).
    /// One definition is shared by every copy and stack of that item.
    [CreateAssetMenu(menuName = "Dark Tower/Item", fileName = "Item")]
    public sealed class ItemDefinition : ScriptableObject, IIdentifiedDefinition
    {
        [SerializeField] private string itemId;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField] private ItemTag tags;
        [SerializeField, Min(1)] private int maxStackSize = 1;
        [SerializeField, Min(1)] private int carrySize = 1;
        [SerializeField, Min(0)] private int sellPriceGold;

        public string Id => itemId;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public ItemTag Tags => tags;
        public int MaxStackSize => maxStackSize;
        public int CarrySize => carrySize;
        public int SellPriceGold => sellPriceGold;

        /// Whether this item appears under the Treasures inventory filter.
        public bool IsTreasure => (tags & ItemTag.Treasure) != 0;
    }
}
