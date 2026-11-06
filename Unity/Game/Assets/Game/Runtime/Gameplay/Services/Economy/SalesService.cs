using UnityEngine;

namespace Game
{
    /// Storehouse selling: a sale always targets one entire stack. Instant Sale removes the stack and
    /// pays half value immediately, with no running record. Normal Sale reserves the stack in place
    /// (it stays in InventoryModel, visible/untouched) and pays full value on a timer; the stack is
    /// only actually removed when that sale completes (ProcessDue) or is rushed (TryRushSale).
    /// Cancelling a Normal sale (TryCancelSale) just drops the running record, leaving the stack as it
    /// was. Only one Running sale may exist per stack at a time.
    public sealed class SalesService : ITimedProcess
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
            if (_session.IsTerminal) return OperationResult<EntityId>.Fail("game_over");

            var stack = _inventory.FindStack(stackId);
            if (stack == null) return OperationResult<EntityId>.Fail("stack_not_found");
            var item = stack.Item;
            if (item == null) return OperationResult<EntityId>.Fail("invalid_stack");
            if (!item.IsSellable) return OperationResult<EntityId>.Fail("item_not_sellable");

            int quantity = stack.Quantity.CurrentValue;
            if (quantity <= 0) return OperationResult<EntityId>.Fail("empty_stack");
            if (HasActiveSale(stackId)) return OperationResult<EntityId>.Fail("stack_already_selling");

            int grossValueGold = SalesRules.TotalSaleValueGold(quantity, item.BaseValueGold);

            if (kind == SaleKind.Instant)
            {
                var removal = _inventory.TryRemoveStack(stackId);
                if (!removal.Success) return OperationResult<EntityId>.Fail(removal.Reason);

                var rules = Db.StorehouseRules;
                float multiplier = rules != null ? rules.InstantSaleMultiplier : 0.5f;
                int payout = SalesRules.RoundSalePrice(grossValueGold * multiplier, rules?.PriceRoundingRule ?? SaleRoundingRule.Floor);
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

        /// Completes every due normal sale at or before nowHours: removes the (still-reserved) stack
        /// and credits its full value.
        public void ProcessDue(float nowHours)
        {
            var sales = _session.Economy.ActiveSales;
            for (int i = sales.Count - 1; i >= 0; i--)
            {
                var sale = sales[i];
                if (sale.Status.CurrentValue != ActivityStatus.Running) continue;
                if (sale.DueGameHours > nowHours) continue;
                _inventory.TryRemoveStack(sale.StackId);
                sale.Complete();
                _economy.CreditGold(sale.GrossValueGold);
                _session.Economy.RemoveSale(sale);
            }
        }

        /// Cancels a running Normal sale. The stack was never removed, so this alone restores idle.
        public OperationResult TryCancelSale(EntityId saleId)
        {
            var sale = FindRunningSale(saleId);
            if (sale == null) return OperationResult.Fail("sale_not_found");

            sale.Cancel();
            _session.Economy.RemoveSale(sale);
            return OperationResult.Ok;
        }

        /// Rushes a running Normal sale: removes the stack now and pays the same half-value payout
        /// as an upfront Instant Sale, using the catalog's configured multiplier/rounding.
        public OperationResult<int> TryRushSale(EntityId saleId)
        {
            var sale = FindRunningSale(saleId);
            if (sale == null) return OperationResult<int>.Fail("sale_not_found");

            var removal = _inventory.TryRemoveStack(sale.StackId);
            if (!removal.Success) return OperationResult<int>.Fail(removal.Reason);

            var rules = Db.StorehouseRules;
            float multiplier = rules != null ? rules.InstantSaleMultiplier : 0.5f;
            int payout = SalesRules.RoundSalePrice(sale.GrossValueGold * multiplier, rules?.PriceRoundingRule ?? SaleRoundingRule.Floor);

            _economy.CreditGold(payout);
            sale.Complete();
            _session.Economy.RemoveSale(sale);
            return OperationResult<int>.Ok(payout);
        }

        private bool HasActiveSale(EntityId stackId)
        {
            var sales = _session.Economy.ActiveSales;
            for (int i = 0; i < sales.Count; i++)
            {
                if (sales[i].StackId.Equals(stackId) && sales[i].Status.CurrentValue == ActivityStatus.Running) return true;
            }
            return false;
        }

        private SaleOperationModel FindRunningSale(EntityId saleId)
        {
            var sales = _session.Economy.ActiveSales;
            for (int i = 0; i < sales.Count; i++)
            {
                if (sales[i].Id.Equals(saleId) && sales[i].Status.CurrentValue == ActivityStatus.Running) return sales[i];
            }
            return null;
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
    }
}
