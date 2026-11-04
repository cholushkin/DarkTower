using System;
using System.Collections.Generic;
using ObservableCollections;
using R3;
using UnityEngine;

namespace Game
{
    public sealed class StorehouseView : MonoBehaviour, IDisposable
    {
        [SerializeField] private RectTransform _content;
        [SerializeField] private StorehouseSlotView _slotPrefab;
        [SerializeField] private StorehouseItemView _itemPrefab;

        private readonly List<StorehouseItemView> _itemViews = new();
        private readonly List<StorehouseSlotView> _emptySlots = new();
        private readonly CompositeDisposable _disposables = new();

        private int _capacity;

        public void Bind(StorehouseViewModel viewModel)
        {
            Clear();

            _capacity = viewModel.SlotCapacity.CurrentValue;
            foreach (var stack in viewModel.Items) AddItemView(_itemViews.Count, stack);
            SyncEmptySlots();

            viewModel.Items.ObserveAdd()
                .Subscribe(e =>
                {
                    AddItemView(e.Index, e.Value);
                    SyncEmptySlots();
                })
                .AddTo(_disposables);

            viewModel.Items.ObserveRemove()
                .Subscribe(e =>
                {
                    RemoveItemView(e.Index);
                    SyncEmptySlots();
                })
                .AddTo(_disposables);

            viewModel.Items.ObserveReset()
                .Subscribe(_ =>
                {
                    ClearItemViews();
                    SyncEmptySlots();
                })
                .AddTo(_disposables);

            viewModel.SlotCapacity
                .Subscribe(capacity =>
                {
                    _capacity = capacity;
                    SyncEmptySlots();
                })
                .AddTo(_disposables);
        }

        public void Dispose()
        {
            _disposables.Dispose();
            ClearItemViews();
            ClearEmptySlots();
        }

        private void AddItemView(int index, ItemStackModel stack)
        {
            var itemView = Instantiate(_itemPrefab, _content);
            itemView.Bind(stack);
            itemView.transform.SetSiblingIndex(index);
            _itemViews.Insert(index, itemView);
        }

        private void RemoveItemView(int index)
        {
            var itemView = _itemViews[index];
            _itemViews.RemoveAt(index);
            itemView.Dispose();
            if (itemView != null) Destroy(itemView.gameObject);
        }

        private void SyncEmptySlots()
        {
            var desired = Math.Max(0, _capacity - _itemViews.Count);

            while (_emptySlots.Count < desired)
            {
                var slot = Instantiate(_slotPrefab, _content);
                _emptySlots.Add(slot);
            }

            while (_emptySlots.Count > desired)
            {
                var last = _emptySlots[^1];
                _emptySlots.RemoveAt(_emptySlots.Count - 1);
                if (last != null) Destroy(last.gameObject);
            }
        }

        private void ClearItemViews()
        {
            foreach (var itemView in _itemViews)
            {
                itemView.Dispose();
                if (itemView != null) Destroy(itemView.gameObject);
            }
            _itemViews.Clear();
        }

        private void ClearEmptySlots()
        {
            foreach (var slot in _emptySlots)
            {
                if (slot != null) Destroy(slot.gameObject);
            }
            _emptySlots.Clear();
        }

        private void Clear()
        {
            _itemViews.Clear();
            _emptySlots.Clear();

            for (int i = _content.childCount - 1; i >= 0; i--)
            {
                var child = _content.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
        }
    }
}
