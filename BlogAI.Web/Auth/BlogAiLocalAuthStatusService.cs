using VsMcpBridge.Shared.AdventuresAuth;
using VsMcpBridge.Shared.BlogAI.Auth;

namespace BlogAI.Web.Auth;

public sealed class BlogAiLocalAuthStatusService : IBlogAiLocalAuthStatusService
{
    private const string ClientApplication = "BlogAI";
    private const string EnvironmentName = "LocalDevelopment";
    private const string ProtectedResourceName = "BlogAI.LocalDev.ProtectedPlaceholder";
    private const string LocalDevelopmentMarker = "local-dev-credential";

    private readonly IBlogAiAuthConsumerService _authConsumerService;

    public BlogAiLocalAuthStatusService(IBlogAiAuthConsumerService authConsumerService)
    {
        _authConsumerService = authConsumerService;
    }

    public BlogAiLocalAuthStatus GetStatus()
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
}
