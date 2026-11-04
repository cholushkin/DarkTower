using System;
using ObservableCollections;
using R3;

public class NotificationWidgetViewModel : IDisposable
{
    private readonly NotificationService _service;
    private readonly CompositeDisposable _disposables = new();
    
    private readonly ObservableList<NotificationItemViewModel> _messages = new();
    
    // View should only observe, never add/remove directly
    public IObservableCollection<NotificationItemViewModel> Messages => _messages;
    

    public NotificationWidgetViewModel(NotificationService service)
    {
        _service = service;

        foreach (var message in _service.Messages)
        {
            _messages.Add(new NotificationItemViewModel(message));
        }

        _service.Messages.ObserveAdd()
            .Subscribe(e => 
            {
                _messages.Insert(e.Index, new NotificationItemViewModel(e.Value));
            })
            .AddTo(_disposables);

        _service.Messages.ObserveRemove()
            .Subscribe(e => 
            {
                var viewModel = _messages[e.Index];
                _messages.RemoveAt(e.Index);
                viewModel.Dispose();
            })
            .AddTo(_disposables);

        _service.Messages.ObserveReset()
            .Subscribe(_ => 
            {
                foreach (var vm in _messages) vm.Dispose();
                _messages.Clear();
            })
            .AddTo(_disposables);
    }

    public void Dispose()
    {
        _disposables.Dispose();
        foreach (var vm in _messages) vm.Dispose();
        _messages.Clear();
    }
}