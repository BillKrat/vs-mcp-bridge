using System;
using System.Linq;
using VsMcpBridge.Shared.AdventuresAuth;
using Xunit;

namespace VsMcpBridge.Shared.Tests;

public sealed class AdventuresAuthTests
{
    [Fact]
    public void Login_without_credential_denies_access()
    {
        var service = new AdventuresAuthDecisionService();
        var request = CreateRequest();

        var decision = service.Login(request);

        Assert.False(decision.Allowed);
        Assert.Equal("Denied", decision.Outcome);
        Assert.Equal("DevCredentialRejected", decision.ReasonCode);
        Assert.Null(decision.Principal);
        Assert.Null(decision.LocalSessionId);
        Assert.Contains(decision.AuditEvents, auditEvent => auditEvent.EventName == AdventuresAuthEventNames.RequestReceived);
        Assert.Contains(decision.AuditEvents, auditEvent => auditEvent.EventName == AdventuresAuthEventNames.LoginEvaluated);
        Assert.Contains(decision.AuditEvents, auditEvent => auditEvent.EventName == AdventuresAuthEventNames.AccessDenied);
    }

    [Fact]
    public void Login_with_valid_development_credential_allows_access()
    {
        var service = new AdventuresAuthDecisionService();
        var request = CreateRequest(devCredential: "local-dev-credential");

        var decision = service.Login(request);

        Assert.True(decision.Allowed);
        Assert.Equal("Allowed", decision.Outcome);
        Assert.Equal("DevCredentialAccepted", decision.ReasonCode);
        Assert.NotNull(decision.Principal);
        Assert.Equal("local-dev-user", decision.Principal!.Subject);
        Assert.Equal("DevelopmentOnly", decision.Principal.Claims["authMode"]);
        Assert.Equal("BlogAI", decision.Principal.Claims["clientApplication"]);
        Assert.Equal("local-session-auth-1", decision.LocalSessionId);
        Assert.Contains(decision.AuditEvents, auditEvent => auditEvent.EventName == AdventuresAuthEventNames.AccessAllowed);
    }

    [Fact]
    public void Login_with_invalid_development_credential_denies_access()
    {
        var service = new AdventuresAuthDecisionService();
        var request = CreateRequest(devCredential: "invalid-local-credential");

        var decision = service.Login(request);

        Assert.False(decision.Allowed);
        Assert.Equal("DevCredentialRejected", decision.ReasonCode);
        Assert.Null(decision.Principal);
        Assert.Null(decision.LocalSessionId);
        Assert.Contains(decision.AuditEvents, auditEvent => auditEvent.EventName == AdventuresAuthEventNames.AccessDenied);
    }

    [Fact]
    public void ValidateSession_with_active_development_session_allows_access()
    {
        var service = new AdventuresAuthDecisionService();
        var login = service.Login(CreateRequest(devCredential: "local-dev-credential"));
        var validateRequest = CreateRequest(authDecisionId: "auth-2", localSessionId: login.LocalSessionId);

        var decision = service.ValidateSession(validateRequest);

        Assert.True(decision.Allowed);
        Assert.Equal("LocalSessionAccepted", decision.ReasonCode);
        Assert.NotNull(decision.Principal);
        Assert.Equal(login.LocalSessionId, decision.LocalSessionId);
        Assert.Contains(decision.AuditEvents, auditEvent => auditEvent.EventName == AdventuresAuthEventNames.SessionValidated);
        AssertCorrelation(validateRequest.Correlation, decision);
    }

    [Fact]
    public void ValidateSession_with_invalid_development_session_denies_access()
    {
        var service = new AdventuresAuthDecisionService();
        var request = CreateRequest(localSessionId: "invalid-session-token");

        var decision = service.ValidateSession(request);

        Assert.False(decision.Allowed);
        Assert.Equal("LocalSessionRejected", decision.ReasonCode);
        Assert.Null(decision.Principal);
        Assert.Null(decision.LocalSessionId);
        Assert.Contains(decision.AuditEvents, auditEvent => auditEvent.EventName == AdventuresAuthEventNames.AccessDenied);
    }

    [Fact]
    public void Logout_invalidates_local_session_placeholder()
    {
        var service = new AdventuresAuthDecisionService();
        var login = service.Login(CreateRequest(devCredential: "local-dev-credential"));
        var logout = service.Logout(CreateRequest(authDecisionId: "auth-logout", localSessionId: login.LocalSessionId));
        var validateAfterLogout = service.ValidateSession(CreateRequest(authDecisionId: "auth-after-logout", localSessionId: login.LocalSessionId));

        Assert.True(logout.Allowed);
        Assert.Equal("LocalSessionInvalidated", logout.ReasonCode);
        Assert.False(validateAfterLogout.Allowed);
        Assert.Equal("LocalSessionRejected", validateAfterLogout.ReasonCode);
    }

    [Fact]
    public void CurrentPrincipal_returns_redacted_local_principal_placeholder()
    {
        var service = new AdventuresAuthDecisionService();
        var login = service.Login(CreateRequest(devCredential: "local-dev-credential"));

        var decision = service.CurrentPrincipal(CreateRequest(authDecisionId: "auth-me", localSessionId: login.LocalSessionId));

        Assert.True(decision.Allowed);
        Assert.Equal("CurrentPrincipalReturned", decision.ReasonCode);
        Assert.NotNull(decision.Principal);
        Assert.Equal("local-dev-user", decision.Principal!.Subject);
        Assert.Equal("Local Development User", decision.Principal.DisplayName);
    }

    [Fact]
    public void Decision_preserves_correlation_metadata()
    {
        var service = new AdventuresAuthDecisionService();
        var correlation = new AdventuresAuthCorrelation(
            "corr-123",
            "request-456",
            "auth-789",
            "BlogAI",
            "LocalDevelopment");
        var request = new AdventuresAuthRequest(correlation, devCredential: "local-dev-credential");

        var decision = service.Login(request);

        AssertCorrelation(correlation, decision);
        Assert.All(
            decision.AuditEvents,
            auditEvent =>
            {
                Assert.Equal("corr-123", auditEvent.Metadata["CorrelationId"]);
                Assert.Equal("request-456", auditEvent.Metadata["RequestId"]);
                Assert.Equal("auth-789", auditEvent.Metadata["AuthDecisionId"]);
                Assert.Equal("BlogAI", auditEvent.Metadata["ClientApplication"]);
                Assert.Equal("LocalDevelopment", auditEvent.Metadata["Environment"]);
            });
    }

    [Fact]
    public void Audit_events_redact_secret_like_values()
    {
        var service = new AdventuresAuthDecisionService();
        var request = CreateRequest(devCredential: "token=raw-token-secret password=raw-password-secret Authorization: Bearer raw-bearer-secret");

        var decision = service.Login(request);

        Assert.Contains(decision.AuditEvents, auditEvent => auditEvent.EventName == AdventuresAuthEventNames.SecretRedacted);
        var auditText = string.Join(
            " ",
            decision.AuditEvents.SelectMany(auditEvent => auditEvent.Metadata.Values));
        Assert.DoesNotContain("raw-token-secret", auditText, StringComparison.Ordinal);
        Assert.DoesNotContain("raw-password-secret", auditText, StringComparison.Ordinal);
        Assert.DoesNotContain("raw-bearer-secret", auditText, StringComparison.Ordinal);
        Assert.Contains("[REDACTED]", auditText, StringComparison.Ordinal);
    }

    [Fact]
    public void Prototype_uses_no_persistence_or_blogengine_dependency()
    {
        var service = new AdventuresAuthDecisionService();

        Assert.False(service.UsesPersistence);
        Assert.Equal("LocalInMemoryOnly", service.StorageKind);
        Assert.DoesNotContain(
            typeof(AdventuresAuthDecisionService).Assembly.GetReferencedAssemblies(),
            reference => reference.Name != null && reference.Name.Contains("BlogEngine", StringComparison.OrdinalIgnoreCase));
    }

    private static AdventuresAuthRequest CreateRequest(
        string correlationId = "corr-1",
        string requestId = "request-1",
        string authDecisionId = "auth-1",
        string clientApplication = "BlogAI",
        string environment = "LocalDevelopment",
        string? devCredential = null,
        string? localSessionId = null)
    {
        return new AdventuresAuthRequest(
            new AdventuresAuthCorrelation(
                correlationId,
                requestId,
                authDecisionId,
                clientApplication,
                environment),
            devCredential,
            localSessionId);
    }

    private static void AssertCorrelation(
        AdventuresAuthCorrelation expected,
        AdventuresAuthDecision decision)
    {
        Assert.Equal(expected.CorrelationId, decision.Correlation.CorrelationId);
        Assert.Equal(expected.RequestId, decision.Correlation.RequestId);
        Assert.Equal(expected.AuthDecisionId, decision.Correlation.AuthDecisionId);
        Assert.Equal(expected.ClientApplication, decision.Correlation.ClientApplication);
        Assert.Equal(expected.Environment, decision.Correlation.Environment);
    }
}
