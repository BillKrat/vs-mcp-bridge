namespace BlogAI.Web.Auth;

public sealed class BlogAiLocalAuthDecisionDisplay
{
    public BlogAiLocalAuthDecisionDisplay(
        string label,
        bool allowed,
        string resourceName,
        string outcome,
        string reasonCode,
        string? principalDisplayName,
        string protectedPlaceholderState,
        bool protectedPlaceholderVisible,
        string protectedPlaceholderMessage,
        string correlationId,
        string requestId,
        string authDecisionId,
        string clientApplication,
        string environment)
    {
        Label = label;
        Allowed = allowed;
        ResourceName = resourceName;
        Outcome = outcome;
        ReasonCode = reasonCode;
        PrincipalDisplayName = principalDisplayName;
        ProtectedPlaceholderState = protectedPlaceholderState;
        ProtectedPlaceholderVisible = protectedPlaceholderVisible;
        ProtectedPlaceholderMessage = protectedPlaceholderMessage;
        CorrelationId = correlationId;
        RequestId = requestId;
        AuthDecisionId = authDecisionId;
        ClientApplication = clientApplication;
        Environment = environment;
    }

    public string Label { get; }

    public bool Allowed { get; }

    public string ResourceName { get; }

    public string Outcome { get; }

    public string ReasonCode { get; }

    public string? PrincipalDisplayName { get; }

    public string ProtectedPlaceholderState { get; }

    public bool ProtectedPlaceholderVisible { get; }

    public string ProtectedPlaceholderMessage { get; }

    public string CorrelationId { get; }

    public string RequestId { get; }

    public string AuthDecisionId { get; }

    public string ClientApplication { get; }

    public string Environment { get; }
}
