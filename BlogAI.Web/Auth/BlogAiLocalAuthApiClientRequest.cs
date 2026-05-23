namespace BlogAI.Web.Auth;

public sealed class BlogAiLocalAuthApiClientRequest
{
    public string? CorrelationId { get; set; }

    public string? RequestId { get; set; }

    public string? AuthDecisionId { get; set; }

    public string ClientApplication { get; set; } = "BlogAI";

    public string Environment { get; set; } = "LocalDevelopment";

    public string? DevCredential { get; set; }

    public string? LocalSessionId { get; set; }
}
