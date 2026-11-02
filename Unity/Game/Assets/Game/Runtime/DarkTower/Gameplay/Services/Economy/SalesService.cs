using UnityEngine;

namespace Game
{
    /// Storehouse selling: a sale always targets one entire stack. Normal Sale removes the stack
    /// immediately and pays full value on a timer; Instant Sale removes the stack and pays half value
    /// immediately. Removing the stack at the moment the sale starts (for both kinds) resolves the
    /// catalog's open "when is the stack reserved/consumed" question the simplest way available
    /// without inventing a second "reserved" stack state Task 04 does not model.
    public sealed class SalesService
    {
        private readonly GameSessionModel _session;
        private readonly InventoryService _inventory;
        private readonly EconomyService _economy;
        private readonly ClockService _clock;
        private DarkTowerDatabase Db => _session.Database;

        public SalesService(GameSessionModel session, InventoryService inventory, EconomyService economy, ClockService clock)
        {
            _session = session;
            _inventory = inventory;
            _economy = economy;
            _clock = clock;
        }

        public OperationResult<EntityId> TryStartSale(EntityId stackId, SaleKind kind)
        {
            var stack = _inventory.FindStack(stackId);
            if (stack == null) return OperationResult<EntityId>.Fail("stack_not_found");
            var item = stack.Item;
            if (item == null) return OperationResult<EntityId>.Fail("invalid_stack");

            int quantity = stack.Quantity.CurrentValue;
            if (quantity <= 0) return OperationResult<EntityId>.Fail("empty_stack");
            int grossValueGold = quantity * item.SellPriceGold;

            var removal = _inventory.TryRemoveStack(stackId);
            if (!removal.Success) return OperationResult<EntityId>.Fail(removal.Reason);

            if (kind == SaleKind.Instant)
            {
                var rules = Db.StorehouseRules;
                float multiplier = rules != null ? rules.InstantSaleMultiplier : 0.5f;
                int payout = RoundSalePrice(grossValueGold * multiplier, rules);
                _economy.CreditGold(payout);
                return OperationResult<EntityId>.Ok(EntityId.None);
            }

            float durationHours = ComputeNormalSaleDurationHours(quantity);
            float now = _clock.NowHours;
            var id = _session.IdAllocator.Next();
            var sale = new SaleOperationModel(id, stackId, kind, quantity, grossValueGold, now, now + durationHours);
            _session.Economy.AddSale(sale);
            return OperationResult<EntityId>.Ok(id);
        }

        /// Completes every due normal sale at or before nowHours, crediting its full value.
        public void ProcessDue(float nowHours)
        {
            var sales = _session.Economy.ActiveSales;
            for (int i = sales.Count - 1; i >= 0; i--)
            {
                var sale = sales[i];
                if (sale.Status.CurrentValue != ActivityStatus.Running) continue;
                if (sale.DueGameHours > nowHours) continue;
                sale.Complete();
                _economy.CreditGold(sale.GrossValueGold);
                _session.Economy.RemoveSale(sale);
            }
        }

        public float? NextDueHours()
        {
            float? min = null;
            var sales = _session.Economy.ActiveSales;
            for (int i = 0; i < sales.Count; i++)
            {
                if (sales[i].Status.CurrentValue != ActivityStatus.Running) continue;
                if (min == null || sales[i].DueGameHours < min.Value) min = sales[i].DueGameHours;
            }
            return min;
        }

        private float ComputeNormalSaleDurationHours(int quantity)
        {
            var rules = Db.StorehouseRules;
            if (rules == null) return 0f;
            var rule = rules.SaleDurationRule;
            float baseHours = rule.BaseDuration.ToHours(_clock.GameHoursPerDay);
            float raw = baseHours + rule.DurationPerCopyHours * quantity;
            float factor = UpgradeEffectRules.GetDurationFactor(_session.Tower, Db, UpgradeEffectKind.ReduceSaleDuration);
            return Mathf.Max(0f, raw * factor);
        }

        private static int RoundSalePrice(float value, StorehouseRulesDefinition rules)
        {
            var rule = rules != null ? rules.PriceRoundingRule : SaleRoundingRule.Floor;
            switch (rule)
            {
                case SaleRoundingRule.Ceil: return Mathf.CeilToInt(value);
                case SaleRoundingRule.Round: return Mathf.RoundToInt(value);
                default: return Mathf.FloorToInt(value);
            }
        }
    }
}
