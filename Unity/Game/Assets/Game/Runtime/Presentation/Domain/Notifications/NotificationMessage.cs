using System;

public class NotificationMessage
{
    public string Id { get; }
    public MessageData Data { get; }

    public NotificationMessage(MessageData data)
    {
        Id = Guid.NewGuid().ToString();
        Data = data;
    }
}