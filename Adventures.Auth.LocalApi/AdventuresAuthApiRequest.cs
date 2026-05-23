namespace Adventures.Auth.LocalApi;

public sealed class AdventuresAuthApiRequest
{
    public string? CorrelationId { get; set; }

    public string? RequestId { get; set; }

    public string? AuthDecisionId { get; set; }

    public string? ClientApplication { get; set; }

    public string? Environment { get; set; }

    public string? DevCredential { get; set; }

    public string? LocalSessionId { get; set; }
}
