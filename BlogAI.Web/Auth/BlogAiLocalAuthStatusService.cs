using VsMcpBridge.Shared.AdventuresAuth;
using VsMcpBridge.Shared.BlogAI.Auth;

namespace BlogAI.Web.Auth;

public sealed class BlogAiLocalAuthStatusService : IBlogAiLocalAuthStatusService
{
    public const string InProcessAuthPath = "in-process";
    public const string ApiClientAuthPath = "api-client";

    private const string ClientApplication = "BlogAI";
    private const string EnvironmentName = "LocalDevelopment";
    private const string ProtectedResourceName = "BlogAI.LocalDev.ProtectedPlaceholder";
    private const string LocalDevelopmentMarker = "local-dev-credential";

    private readonly IBlogAiAuthConsumerService _authConsumerService;
    private readonly IBlogAiLocalAuthApiClient _authApiClient;

    public BlogAiLocalAuthStatusService(
        IBlogAiAuthConsumerService authConsumerService,
        IBlogAiLocalAuthApiClient authApiClient)
    {
        _authConsumerService = authConsumerService;
        _authApiClient = authApiClient;
    }

    public async Task<BlogAiLocalAuthStatus> GetStatusAsync(
        string? authPath = null,
        CancellationToken cancellationToken = default)
    {
        if (string.Equals(authPath, ApiClientAuthPath, StringComparison.OrdinalIgnoreCase))
            return await GetApiClientDiagnosticStatusAsync(cancellationToken).ConfigureAwait(false);

        return GetInProcessStatus();
    }

    private BlogAiLocalAuthStatus GetInProcessStatus()
    {
        var unauthenticated = Evaluate(
            "Unauthenticated local request",
            devAuthMarker: null);

        var developmentAuthenticated = Evaluate(
            "Development-authenticated local request",
            devAuthMarker: LocalDevelopmentMarker);

        return new BlogAiLocalAuthStatus(
            ProtectedResourceName,
            ClientApplication,
            EnvironmentName,
            InProcessAuthPath,
            "In-process baseline",
            isDiagnosticMode: false,
            diagnosticMessage: "Default local/dev path. This uses the in-process BlogAI auth consumer boundary.",
            new[] { unauthenticated, developmentAuthenticated });
    }

    private BlogAiLocalAuthDecisionDisplay Evaluate(string label, string? devAuthMarker)
    {
        var request = new BlogAiAuthConsumerRequest(
            ProtectedResourceName,
            requiresAuthentication: true,
            AdventuresAuthCorrelation.Create(ClientApplication, EnvironmentName),
            devAuthMarker);

        var decision = _authConsumerService.EvaluateAccess(request);
        var protectedPlaceholderVisible = decision.Allowed;

        return new BlogAiLocalAuthDecisionDisplay(
            label,
            decision.Allowed,
            decision.ResourceName,
            decision.Outcome,
            decision.ReasonCode,
            decision.Principal?.DisplayName,
            protectedPlaceholderVisible ? "Shown" : "Hidden",
            protectedPlaceholderVisible,
            protectedPlaceholderVisible
                ? "Protected placeholder is shown for this local development decision."
                : "Protected placeholder is denied and hidden for this local development decision.",
            decision.Correlation.CorrelationId,
            decision.Correlation.RequestId,
            decision.Correlation.AuthDecisionId,
            decision.Correlation.ClientApplication,
            decision.Correlation.Environment);
    }

    private async Task<BlogAiLocalAuthStatus> GetApiClientDiagnosticStatusAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            var unauthenticated = await _authApiClient
                .LoginAsync(CreateApiRequest(), cancellationToken)
                .ConfigureAwait(false);
            var developmentAuthenticated = await _authApiClient
                .LoginAsync(CreateApiRequest(devCredential: LocalDevelopmentMarker), cancellationToken)
                .ConfigureAwait(false);

            return new BlogAiLocalAuthStatus(
                ProtectedResourceName,
                ClientApplication,
                EnvironmentName,
                ApiClientAuthPath,
                "API client diagnostic",
                isDiagnosticMode: true,
                diagnosticMessage: "Explicit local/dev diagnostic mode. This exercises the AdventuresAuth Local API client and does not fall back to the in-process path.",
                new[]
                {
                    MapApiDecision("API client unauthenticated local request", unauthenticated),
                    MapApiDecision("API client development-authenticated local request", developmentAuthenticated)
                });
        }
        catch (Exception ex) when (
            ex is HttpRequestException ||
            ex is TaskCanceledException ||
            ex is InvalidOperationException)
        {
            return CreateApiDiagnosticFailureStatus();
        }
    }

    private static BlogAiLocalAuthApiClientRequest CreateApiRequest(string? devCredential = null)
    {
        var correlation = AdventuresAuthCorrelation.Create(ClientApplication, EnvironmentName);

        return new BlogAiLocalAuthApiClientRequest
        {
            CorrelationId = correlation.CorrelationId,
            RequestId = correlation.RequestId,
            AuthDecisionId = correlation.AuthDecisionId,
            ClientApplication = correlation.ClientApplication,
            Environment = correlation.Environment,
            DevCredential = devCredential
        };
    }

    private static BlogAiLocalAuthDecisionDisplay MapApiDecision(
        string label,
        BlogAiLocalAuthApiClientDecision decision)
    {
        var protectedPlaceholderVisible = decision.Allowed;

        return new BlogAiLocalAuthDecisionDisplay(
            label,
            decision.Allowed,
            ProtectedResourceName,
            decision.Outcome,
            decision.ReasonCode,
            decision.Principal?.DisplayName,
            protectedPlaceholderVisible ? "Shown" : "Hidden",
            protectedPlaceholderVisible,
            protectedPlaceholderVisible
                ? "Protected placeholder is shown for this API client diagnostic decision."
                : "Protected placeholder is denied and hidden for this API client diagnostic decision.",
            decision.CorrelationId,
            decision.RequestId,
            decision.AuthDecisionId,
            decision.ClientApplication,
            decision.Environment);
    }

    private static BlogAiLocalAuthStatus CreateApiDiagnosticFailureStatus()
    {
        var correlation = AdventuresAuthCorrelation.Create(ClientApplication, EnvironmentName);

        var failure = new BlogAiLocalAuthDecisionDisplay(
            "API client diagnostic failure",
            allowed: false,
            ProtectedResourceName,
            "DiagnosticFailure",
            "LocalApiUnavailable",
            principalDisplayName: null,
            "Hidden",
            protectedPlaceholderVisible: false,
            "API client diagnostic path failed. No in-process fallback was used.",
            correlation.CorrelationId,
            correlation.RequestId,
            correlation.AuthDecisionId,
            correlation.ClientApplication,
            correlation.Environment);

        return new BlogAiLocalAuthStatus(
            ProtectedResourceName,
            ClientApplication,
            EnvironmentName,
            ApiClientAuthPath,
            "API client diagnostic",
            isDiagnosticMode: true,
            diagnosticMessage: "Explicit local/dev diagnostic mode failed because the AdventuresAuth Local API was unavailable or returned an invalid response. The in-process path was not used as a fallback.",
            new[] { failure });
    }
}
