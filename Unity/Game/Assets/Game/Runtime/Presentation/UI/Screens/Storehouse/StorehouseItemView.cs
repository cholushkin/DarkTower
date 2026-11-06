using System;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    public sealed class StorehouseItemView : MonoBehaviour, IDisposable
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _name;
        [SerializeField] private TMP_Text _quantity;

        [Header("Idle state")]
        [SerializeField] private Button _sellButton;
        [SerializeField] private TMP_Text _sellButtonText;
        [SerializeField] private GameObject _infoButton;
        [SerializeField] private GameObject _infoButtonHighlighted;

        [Header("Selling state")]
        [SerializeField] private GameObject _progressPanel;
        [SerializeField] private TMP_Text _sellNumberText;
        [SerializeField] private TMP_Text _remainingTimeText;
        [SerializeField] private Image _progressFill;
        [SerializeField] private Button _cancelButton;
        [SerializeField] private Button _quickSellButton;
        [SerializeField] private TMP_Text _quickSellPriceText;

        private readonly CompositeDisposable _disposables = new();
        private StorehouseView _owner;
        private StorehouseItemViewModel _viewModel;
        private PointerHoverRelay _sellHoverRelay;
        private PointerHoverRelay _progressHoverRelay;

        public void Bind(StorehouseView owner, StorehouseItemViewModel viewModel)
        {
            _owner = owner;
            _viewModel = viewModel;

            _icon.sprite = viewModel.Item.Icon;
            _name.text = viewModel.Item.DisplayName;
            viewModel.Quantity.Subscribe(SetQuantity).AddTo(_disposables);

            viewModel.TotalSaleValueGold.Subscribe(v =>
            {
                _sellButtonText.text = $"<size=60%>Sell for </size>\n{v}";
                _sellNumberText.text = v.ToString();
            }).AddTo(_disposables);

            viewModel.RemainingHours.Subscribe(h => _remainingTimeText.text = FormatRemaining(h)).AddTo(_disposables);
            viewModel.Progress.Subscribe(p => _progressFill.fillAmount = p).AddTo(_disposables);
            viewModel.QuickSellPayoutGold.Subscribe(v => _quickSellPriceText.text = v.ToString()).AddTo(_disposables);

            viewModel.IsSelling.Subscribe(selling =>
            {
                _sellButton.gameObject.SetActive(!selling);
                _progressPanel.SetActive(selling);
                _cancelButton.gameObject.SetActive(false);
                _quickSellButton.gameObject.SetActive(false);
            }).AddTo(_disposables);

            _sellButton.onClick.AddListener(viewModel.Sell);
            _cancelButton.onClick.AddListener(viewModel.Cancel);
            _quickSellButton.onClick.AddListener(viewModel.QuickSell);

            _sellHoverRelay = _sellButton.gameObject.AddComponent<PointerHoverRelay>();
            _sellHoverRelay.OnEnter += OnSellHoverEnter;
            _sellHoverRelay.OnExit += OnSellHoverExit;

            _progressHoverRelay = _progressPanel.AddComponent<PointerHoverRelay>();
            _progressHoverRelay.OnEnter += OnProgressHoverEnter;
            _progressHoverRelay.OnExit += OnProgressHoverExit;
        }

        public void Dispose()
        {
            _sellButton.onClick.RemoveListener(_viewModel.Sell);
            _cancelButton.onClick.RemoveListener(_viewModel.Cancel);
            _quickSellButton.onClick.RemoveListener(_viewModel.QuickSell);

            if (_sellHoverRelay != null)
            {
                _sellHoverRelay.OnEnter -= OnSellHoverEnter;
                _sellHoverRelay.OnExit -= OnSellHoverExit;
            }

            if (_progressHoverRelay != null)
            {
                _progressHoverRelay.OnEnter -= OnProgressHoverEnter;
                _progressHoverRelay.OnExit -= OnProgressHoverExit;
            }

            _owner?.HideTooltip();
            _disposables.Dispose();
            _viewModel?.Dispose();
        }

        private void OnSellHoverEnter()
        {
            _infoButton.SetActive(false);
            _infoButtonHighlighted.SetActive(true);
            _owner.ShowTooltip(_sellButton.transform as RectTransform, _viewModel.Item.Description);
        }

        private void OnSellHoverExit()
        {
            _infoButton.SetActive(true);
            _infoButtonHighlighted.SetActive(false);
            _owner.HideTooltip();
        }

        private void OnProgressHoverEnter()
        {
            _cancelButton.gameObject.SetActive(true);
            _quickSellButton.gameObject.SetActive(true);
        }

        private void OnProgressHoverExit()
        {
            _cancelButton.gameObject.SetActive(false);
            _quickSellButton.gameObject.SetActive(false);
        }

        private void SetQuantity(int quantity)
        {
            _quantity.text = quantity.ToString();
        }

        private static string FormatRemaining(float hours)
        {
            int totalMinutes = Mathf.CeilToInt(hours * 60f);
            int h = totalMinutes / 60;
            int m = totalMinutes % 60;
            return h > 0 ? $"{h}h {m}m" : $"{m}m";
        }
    }
}
