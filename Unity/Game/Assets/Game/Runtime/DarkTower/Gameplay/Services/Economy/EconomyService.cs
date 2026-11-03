using System.Collections.Generic;
using R3;

namespace Game
{
    /// Gold/Crystals plus combined currency+item cost validation. Normal purchases are all-or-nothing
    /// and never drive a balance negative; item costs are delegated to InventoryService so a mixed
    /// cost (e.g. Gold + a crafting item) is validated together before anything is spent.
    public sealed class EconomyService
    {
        private readonly GameSessionModel _session;
        private readonly InventoryService _inventory;
        private EconomyModel Economy => _session.Economy;

        public EconomyService(GameSessionModel session, InventoryService inventory)
        {
            _session = session;
            _inventory = inventory;
        }

        public ReadOnlyReactiveProperty<int> Gold => Economy.Gold;
        public ReadOnlyReactiveProperty<int> Crystals => Economy.Crystals;

        public bool CanAfford(ResourceCost cost)
        {
            if (Economy.Gold.CurrentValue < cost.Gold) return false;
            if (Economy.Crystals.CurrentValue < cost.Crystals) return false;

            var totals = AggregateItems(cost.Items);
            if (totals == null) return true;
            foreach (var entry in totals)
                if (!_inventory.HasQuantity(entry.Key, entry.Value)) return false;
            return true;
        }

        /// Validates the whole cost (currencies + items) before touching anything, then deducts all
        /// of it. Never partially spends on an unaffordable request. A cost listing the same item in
        /// more than one entry is validated and deducted against their combined total — otherwise
        /// each entry checked in isolation could pass individually while the combined amount is
        /// actually short, leaving a partial deduction once the later entry fails.
        public OperationResult TrySpend(ResourceCost cost)
        {
            if (_session.IsTerminal) return OperationResult.Fail("game_over");
            if (!CanAfford(cost)) return OperationResult.Fail("insufficient_resources");

            if (cost.Gold > 0) Economy.AddGold(-cost.Gold);
            if (cost.Crystals > 0) Economy.AddCrystals(-cost.Crystals);

            var totals = AggregateItems(cost.Items);
            if (totals != null)
                foreach (var entry in totals)
                    _inventory.TryRemoveQuantity(entry.Key, entry.Value);

            return OperationResult.Ok;
        }

        public void CreditGold(int amount) { if (amount > 0) Economy.AddGold(amount); }
        public void CreditCrystals(int amount) { if (amount > 0) Economy.AddCrystals(amount); }

        /// Currency-only spend (no items) for callers that have nothing to build a ResourceCost
        /// from — ResourceCost is a Unity-serialization-only struct with no public constructor, so a
        /// gold-only deduction (e.g. weekly payroll) goes through this instead of fabricating one.
        public OperationResult TrySpendGold(int amount)
        {
            if (_session.IsTerminal) return OperationResult.Fail("game_over");
            if (amount < 0) return OperationResult.Fail("invalid_request");
            if (Economy.Gold.CurrentValue < amount) return OperationResult.Fail("insufficient_resources");
            Economy.AddGold(-amount);
            return OperationResult.Ok;
        }

        /// Combines cost entries for the same item into one total so duplicates are affordability-
        /// checked and deducted as a single amount. Returns null when the cost has no item entries.
        private static Dictionary<ItemDefinition, int> AggregateItems(ItemAmount[] items)
        {
            if (items == null || items.Length == 0) return null;

            Dictionary<ItemDefinition, int> totals = null;
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i].Item == null) continue;
                totals ??= new Dictionary<ItemDefinition, int>();
                totals.TryGetValue(items[i].Item, out var existing);
                totals[items[i].Item] = existing + items[i].Amount;
            }
            return totals;
        }
    }
}
