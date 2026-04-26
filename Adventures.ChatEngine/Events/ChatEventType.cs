namespace Adventures.ChatEngine.Events;

public enum ChatEventType
{
    RequestStarted,
    Cancelled,
    RetryScheduled,
    RetryAttempt,
    RetryExhausted,
    ResponseCompleted,
}
