using System;
using ObservableCollections;
using R3;
using UnityEngine;

namespace Game
{
    /// Per-row presentation state for one storehouse stack: formats the stack's sale price and (while
    /// a Normal sale is running against it) progress/remaining-time/rush-payout, and forwards
    /// Sell/Cancel/QuickSell user intentions to SalesService. One instance per ItemStackModel, owned
    /// and disposed by whichever View creates the row (see StorehouseView/StorehouseViewModel).
    public sealed class StorehouseItemViewModel : IDisposable
    {
        private readonly ItemStackModel _stack;
        private readonly SalesService _sales;
        private readonly ClockService _clock;
        private readonly StorehouseRulesDefinition _rules;
        private readonly CompositeDisposable _disposables = new();

        private readonly ReactiveProperty<int> _totalSaleValueGold;
        private readonly ReactiveProperty<bool> _isSelling;
        private readonly ReactiveProperty<float> _progress = new(0f);
        private readonly ReactiveProperty<float> _remainingHours = new(0f);
        private readonly ReactiveProperty<int> _quickSellPayoutGold = new(0);

        private IDisposable _progressSubscription;
        private SaleOperationModel _activeSale;

        public ItemDefinition Item => _stack.Item;
        public ReadOnlyReactiveProperty<int> Quantity => _stack.Quantity;
        public ReadOnlyReactiveProperty<int> TotalSaleValueGold => _totalSaleValueGold;
        public ReadOnlyReactiveProperty<bool> IsSelling => _isSelling;
        public ReadOnlyReactiveProperty<float> Progress => _progress;
        public ReadOnlyReactiveProperty<float> RemainingHours => _remainingHours;
        public ReadOnlyReactiveProperty<int> QuickSellPayoutGold => _quickSellPayoutGold;

        public bool CanSell => Item != null && Item.IsSellable &&
            _totalSaleValueGold.CurrentValue > 0 && !_isSelling.CurrentValue;

        public StorehouseItemViewModel(ItemStackModel stack, EconomyModel economy, SalesService sales,
            ClockService clock, StorehouseRulesDefinition rules)
        {
            _stack = stack;
            _sales = sales;
            _clock = clock;
            _rules = rules;

            _totalSaleValueGold = new ReactiveProperty<int>(ComputeTotalSaleValue());
            _isSelling = new ReactiveProperty<bool>(false);

            AddDisposables(_totalSaleValueGold, _isSelling, _progress, _remainingHours, _quickSellPayoutGold);

            _stack.Quantity.Subscribe(_ => _totalSaleValueGold.Value = ComputeTotalSaleValue()).AddTo(_disposables);

            SetActiveSale(FindActiveSale(economy));

            economy.ActiveSales.ObserveAdd()
                .Subscribe(e => { if (e.Value.StackId.Equals(_stack.Id)) SetActiveSale(e.Value); })
                .AddTo(_disposables);

            economy.ActiveSales.ObserveRemove()
                .Subscribe(e => { if (_activeSale == e.Value) SetActiveSale(null); })
                .AddTo(_disposables);

            economy.ActiveSales.ObserveReset()
                .Subscribe(_ => SetActiveSale(null))
                .AddTo(_disposables);
        }

        public void Sell() => _sales.TryStartSale(_stack.Id, SaleKind.Normal);

        public void QuickSell()
        {
            if (_activeSale != null) _sales.TryRushSale(_activeSale.Id);
        }

        public void Cancel()
        {
            if (_activeSale != null) _sales.TryCancelSale(_activeSale.Id);
        }

        public void Dispose()
        {
            _progressSubscription?.Dispose();
            _disposables.Dispose();
        }

        private SaleOperationModel FindActiveSale(EconomyModel economy)
        {
            var sales = economy.ActiveSales;
            for (int i = 0; i < sales.Count; i++)
            {
                if (sales[i].StackId.Equals(_stack.Id) && sales[i].Status.CurrentValue == ActivityStatus.Running)
                    return sales[i];
            }
            return null;
        }

        private void SetActiveSale(SaleOperationModel sale)
        {
            _activeSale = sale;
            _progressSubscription?.Dispose();
            _progressSubscription = null;

            _isSelling.Value = sale != null;

            if (sale == null)
            {
                _progress.Value = 0f;
                _remainingHours.Value = 0f;
                _quickSellPayoutGold.Value = 0;
                return;
            }

            float multiplier = _rules != null ? _rules.InstantSaleMultiplier : 0.5f;
            var roundingRule = _rules != null ? _rules.PriceRoundingRule : SaleRoundingRule.Floor;
            _quickSellPayoutGold.Value = SalesRules.RoundSalePrice(sale.GrossValueGold * multiplier, roundingRule);

            UpdateProgress(sale, _clock.NowHours);
            _progressSubscription = _clock.TotalGameHours.Subscribe(now => UpdateProgress(sale, now));
        }

        private void UpdateProgress(SaleOperationModel sale, float nowHours)
        {
            float span = sale.DueGameHours - sale.StartGameHours;
            float progress = span > 0f ? (nowHours - sale.StartGameHours) / span : 1f;
            _progress.Value = Mathf.Clamp01(progress);
            _remainingHours.Value = Mathf.Max(0f, sale.DueGameHours - nowHours);
        }

        private int ComputeTotalSaleValue() =>
            Item == null ? 0 : SalesRules.TotalSaleValueGold(Quantity.CurrentValue, Item.BaseValueGold);

        private void AddDisposables(params IDisposable[] disposables)
        {
            foreach (var d in disposables) _disposables.Add(d);
        }
    }
}
