namespace BlogAI.Web.Auth;

public sealed class BlogAiLocalAuthApiClientDecision
{
    public bool Allowed { get; set; }

    public string Outcome { get; set; } = string.Empty;

    public string ReasonCode { get; set; } = string.Empty;

    public string CorrelationId { get; set; } = string.Empty;

    public string RequestId { get; set; } = string.Empty;

    public string AuthDecisionId { get; set; } = string.Empty;

    public string ClientApplication { get; set; } = string.Empty;

    public string Environment { get; set; } = string.Empty;

    public BlogAiLocalAuthApiClientPrincipal? Principal { get; set; }

    public string? LocalSessionId { get; set; }

    public string[] AuditEventNames { get; set; } = Array.Empty<string>();

    public bool SecretsRedacted { get; set; }

    public string StorageKind { get; set; } = string.Empty;
}
