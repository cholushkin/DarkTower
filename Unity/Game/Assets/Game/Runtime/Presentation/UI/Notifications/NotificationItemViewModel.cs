using System;
using UnityEngine;

public class NotificationItemViewModel : IDisposable
{
    public string Id { get; }
    public string Message { get; }
    public MessageLevel Level { get; }
    public Sprite TargetItem { get; }
    public Color Color { get; }

    public NotificationItemViewModel(NotificationMessage model)
    {
        Id = model.Id;
        Message = model.Data.Message;
        Level = model.Data.Level;
        TargetItem = model.Data.TargetItem;
        Color = model.Data.Color;
    }

    public void Dispose()
    {
        // Reserved for any inner subscription cleanup if items become dynamic later
    }
}