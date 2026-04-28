namespace Adventures.ChatEngine.Events;

public sealed record ChatEvent(ChatEventType Type, string? Content = null);
