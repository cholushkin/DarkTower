using System.Collections.Generic;

namespace Game
{
    /// Shared slot-accounting rule for Raid Report selections: copies of the same item stack by its
    /// max_stack_size (identical to Storehouse) before costing a carry slot. The single source of
    /// truth for "how many slots does this selection need", used by RaidModel.SelectedCarryUsed,
    /// RaidClaimPolicy.DefaultPreselect, and RaidService.TryClaimRaid's budget check.
    public static class RaidCarryRules
    {
        public static int SlotsUsed(IReadOnlyList<ItemDefinition> items)
        {
            var countsByItem = new Dictionary<ItemDefinition, int>();
            if (items != null)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    if (item == null) continue;
                    countsByItem.TryGetValue(item, out var count);
                    countsByItem[item] = count + 1;
                }
            }

            int slots = 0;
            foreach (var kvp in countsByItem) slots += StorageRules.StacksFor(kvp.Value, kvp.Key.MaxStackSize);
            return slots;
        }
    }
}
