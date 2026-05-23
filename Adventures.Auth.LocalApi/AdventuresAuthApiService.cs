using VsMcpBridge.Shared.AdventuresAuth;

namespace Adventures.Auth.LocalApi;

public sealed class AdventuresAuthApiService : IAdventuresAuthApiService
{
    private readonly IAdventuresAuthDecisionService _decisionService;

    public AdventuresAuthApiService(IAdventuresAuthDecisionService decisionService)
    {
        _decisionService = decisionService ?? throw new ArgumentNullException(nameof(decisionService));
    }

    public bool UsesPersistence => false;

    public string StorageKind => "None";

    public AdventuresAuthApiResponse Login(AdventuresAuthApiRequest request)
    {
        return ToResponse(_decisionService.Login(ToAuthRequest(request)));
    }

    public AdventuresAuthApiResponse Logout(AdventuresAuthApiRequest request)
    {
        return ToResponse(_decisionService.Logout(ToAuthRequest(request)));
    }

    public AdventuresAuthApiResponse CurrentPrincipal(AdventuresAuthApiRequest request)
    {
        return ToResponse(_decisionService.CurrentPrincipal(ToAuthRequest(request)));
    }

    public AdventuresAuthApiResponse Validate(AdventuresAuthApiRequest request)
    {
        return ToResponse(_decisionService.ValidateSession(ToAuthRequest(request)));
    }

    private static AdventuresAuthRequest ToAuthRequest(AdventuresAuthApiRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        return new AdventuresAuthRequest(
            CreateCorrelation(request),
            request.DevCredential,
            request.LocalSessionId);
    }

    private static AdventuresAuthCorrelation CreateCorrelation(AdventuresAuthApiRequest request)
    {
        return new AdventuresAuthCorrelation(
            GetOrCreateIdentifier(request.CorrelationId, "corr"),
            GetOrCreateIdentifier(request.RequestId, "request"),
            GetOrCreateIdentifier(request.AuthDecisionId, "auth"),
            string.IsNullOrWhiteSpace(request.ClientApplication) ? "BlogAI" : request.ClientApplication!,
            string.IsNullOrWhiteSpace(request.Environment) ? "LocalDevelopment" : request.Environment!);
    }

    private static string GetOrCreateIdentifier(string? value, string prefix)
    {
        return string.IsNullOrWhiteSpace(value)
            ? prefix + "-" + Guid.NewGuid().ToString("N")
            : value!;
    }

    private static AdventuresAuthApiResponse ToResponse(AdventuresAuthDecision decision)
    {
        return new AdventuresAuthApiResponse
        {
            Allowed = decision.Allowed,
            Outcome = decision.Outcome,
            ReasonCode = decision.ReasonCode,
            CorrelationId = decision.Correlation.CorrelationId,
            RequestId = decision.Correlation.RequestId,
            AuthDecisionId = decision.Correlation.AuthDecisionId,
            ClientApplication = decision.Correlation.ClientApplication,
            Environment = decision.Correlation.Environment,
            Principal = AdventuresAuthApiPrincipal.FromPrincipal(decision.Principal),
            LocalSessionId = decision.LocalSessionId,
            AuditEventNames = decision.AuditEvents.Select(auditEvent => auditEvent.EventName).ToArray(),
            SecretsRedacted = decision.AuditEvents.Any(auditEvent =>
                auditEvent.EventName == AdventuresAuthEventNames.SecretRedacted),
            StorageKind = "None"
        };
    }
}
