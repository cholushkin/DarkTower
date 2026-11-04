// todo: Evaluate if Color should be strictly UI-side mapping based on MessageLevel, rather than passed from Domain.

using UnityEngine;

public enum MessageLevel
{
    Info,
    Important
}

public struct MessageData
{
    public MessageLevel Level;
    public Sprite TargetItem;
    public string Message;
    public Color Color;

    public MessageData(string message, MessageLevel level = MessageLevel.Info, Sprite targetItem = null, Color? color = null)
    {
        Message = message;
        Level = level;
        TargetItem = targetItem;
        Color = color ?? Color.white;
    }
}