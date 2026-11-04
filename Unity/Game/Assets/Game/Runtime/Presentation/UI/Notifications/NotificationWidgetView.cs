using System;
using System.Collections.Generic;
using ObservableCollections; // Required for ObserveAdd/Remove extensions
using R3;
using TMPro;
using UnityEngine;

public class NotificationWidgetView : MonoBehaviour, IDisposable
{
    [Header("Messages Feed")]
    [SerializeField] private NotificationItemView _itemPrefab;
    [SerializeField] private RectTransform _messagesContainer;

    private readonly CompositeDisposable _disposables = new();
    private readonly Dictionary<string, NotificationItemView> _activeViews = new();

    public void Bind(NotificationWidgetViewModel viewModel)
    {
        // Subscriptions now correctly recognized
        viewModel.Messages.ObserveAdd()
            .Subscribe(e => AddMessage(e.Value))
            .AddTo(_disposables);

        viewModel.Messages.ObserveRemove()
            .Subscribe(e => RemoveMessage(e.Value))
            .AddTo(_disposables);

        viewModel.Messages.ObserveReset()
            .Subscribe(_ => ClearMessages())
            .AddTo(_disposables);

        foreach (var msg in viewModel.Messages)
        {
            AddMessage(msg);
        }
    }

    private void AddMessage(NotificationItemViewModel itemViewModel)
    {
        if (_itemPrefab == null || _messagesContainer == null) return;

        var view = Instantiate(_itemPrefab, _messagesContainer);
        view.Bind(itemViewModel);
        _activeViews[itemViewModel.Id] = view;
    }

    private void RemoveMessage(NotificationItemViewModel itemViewModel)
    {
        if (_activeViews.TryGetValue(itemViewModel.Id, out var view))
        {
            if (view != null) Destroy(view.gameObject);
            _activeViews.Remove(itemViewModel.Id);
        }
    }

    private void ClearMessages()
    {
        foreach (var view in _activeViews.Values)
        {
            if (view != null) Destroy(view.gameObject);
        }
        _activeViews.Clear();
    }

    public void Dispose()
    {
        _disposables.Dispose();
        ClearMessages();
    }
}