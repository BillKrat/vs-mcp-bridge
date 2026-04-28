namespace Adventures.ChatEngine.Events;

public enum ChatEventType
{
    RequestStarted,
    TokenGenerated,
    Cancelled,
    RetryScheduled,
    RetryAttempt,
    RetryExhausted,
    ResponseCompleted,
}
