using System;
using ObservableCollections;
using R3;

public sealed class NotificationService : IDisposable
{
    private readonly ObservableList<NotificationMessage> _messages = new();
    public IObservableCollection<NotificationMessage> Messages => _messages;


    public void AddMessage(MessageData data)
    {
        _messages.Add(new NotificationMessage(data));
    }

    public void RemoveMessage(NotificationMessage message)
    {
        _messages.Remove(message);
    }


    public void Dispose()
    {
        _messages.Clear();
    }
}