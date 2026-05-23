using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using VsMcpBridge.Shared.AdventuresAuth;
using VsMcpBridge.Shared.BlogAI.Auth;
using VsMcpBridge.Shared.Composition;
using Xunit;

namespace VsMcpBridge.Shared.Tests;

public sealed class BlogAiAuthConsumerTests
{
    [Fact]
    public void Protected_resource_denies_unauthenticated_request()
    {
        var service = CreateService();
        var request = CreateRequest();

        var decision = service.EvaluateAccess(request);

        Assert.False(decision.Allowed);
        Assert.Equal(BlogAiProtectedResourceAccessDecision.Denied, decision.AccessDecision);
        Assert.Equal("Denied", decision.Outcome);
        Assert.Equal("LocalSessionRejected", decision.ReasonCode);
        Assert.Null(decision.Principal);
        Assert.NotNull(decision.AuthDecision);
        Assert.Contains(
            decision.AuthDecision!.AuditEvents,
            auditEvent => auditEvent.EventName == AdventuresAuthEventNames.AccessDenied);
    }

    [Fact]
    public void Protected_resource_allows_valid_local_development_auth()
    {
        var service = CreateService();
        var request = CreateRequest(devAuthMarker: "local-dev-credential");

        var decision = service.EvaluateAccess(request);

        Assert.True(decision.Allowed);
        Assert.Equal(BlogAiProtectedResourceAccessDecision.Allowed, decision.AccessDecision);
        Assert.Equal("Allowed", decision.Outcome);
        Assert.Equal("DevCredentialAccepted", decision.ReasonCode);
        Assert.NotNull(decision.Principal);
        Assert.Equal("local-dev-user", decision.Principal!.Subject);
        Assert.NotNull(decision.AuthDecision);
        Assert.NotNull(decision.AuthDecision!.LocalSessionId);
        Assert.Contains(
            decision.AuthDecision.AuditEvents,
            auditEvent => auditEvent.EventName == AdventuresAuthEventNames.AccessAllowed);
    }

    [Fact]
    public void Protected_resource_denies_invalid_local_development_auth()
    {
        var service = CreateService();
        var request = CreateRequest(devAuthMarker: "invalid-local-credential");

        var decision = service.EvaluateAccess(request);

        Assert.False(decision.Allowed);
        Assert.Equal("Denied", decision.Outcome);
        Assert.Equal("DevCredentialRejected", decision.ReasonCode);
        Assert.Null(decision.Principal);
        Assert.NotNull(decision.AuthDecision);
        Assert.Null(decision.AuthDecision!.LocalSessionId);
    }

    [Fact]
    public void Protected_resource_denies_invalid_local_session()
    {
        var service = CreateService();
        var request = CreateRequest(localSessionId: "invalid-session-token");

        var decision = service.EvaluateAccess(request);

        Assert.False(decision.Allowed);
        Assert.Equal("LocalSessionRejected", decision.ReasonCode);
        Assert.Null(decision.Principal);
        Assert.NotNull(decision.AuthDecision);
        Assert.Null(decision.AuthDecision!.LocalSessionId);
        Assert.Contains(
            decision.AuthDecision.AuditEvents,
            auditEvent => auditEvent.EventName == AdventuresAuthEventNames.SessionValidated);
    }

    [Fact]
    public void Public_resource_allows_without_auth()
    {
        var service = CreateService();
        var request = CreateRequest(resourceName: "/blogAi/public", requiresAuthentication: false);

        var decision = service.EvaluateAccess(request);

        Assert.True(decision.Allowed);
        Assert.Equal(BlogAiProtectedResourceAccessDecision.Allowed, decision.AccessDecision);
        Assert.Equal("PublicResource", decision.ReasonCode);
        Assert.Null(decision.AuthDecision);
        Assert.Null(decision.Principal);
        Assert.Equal("Public", decision.Metadata["ResourceCategory"]);
    }

    [Fact]
    public void Correlation_metadata_preserved_from_blogai_to_adventuresauth()
    {
        var service = CreateService();
        var correlation = new AdventuresAuthCorrelation(
            "corr-blogai-123",
            "request-blogai-456",
            "auth-blogai-789",
            "BlogAI",
            "LocalDevelopment");
        var request = CreateRequest(correlation: correlation, devAuthMarker: "local-dev-credential");

        var decision = service.EvaluateAccess(request);

        AssertCorrelation(correlation, decision.Correlation);
        Assert.Equal("corr-blogai-123", decision.Metadata["CorrelationId"]);
        Assert.Equal("request-blogai-456", decision.Metadata["RequestId"]);
        Assert.Equal("auth-blogai-789", decision.Metadata["AuthDecisionId"]);
        Assert.Equal("BlogAI", decision.Metadata["ClientApplication"]);
        Assert.Equal("LocalDevelopment", decision.Metadata["Environment"]);
        Assert.NotNull(decision.AuthDecision);
        AssertCorrelation(correlation, decision.AuthDecision!.Correlation);
        Assert.All(
            decision.AuthDecision.AuditEvents,
            auditEvent => AssertCorrelation(correlation, auditEvent.Correlation));
    }

    [Fact]
    public void Decision_metadata_and_audit_evidence_redact_secret_like_values()
    {
        var service = CreateService();
        var request = CreateRequest(
            devAuthMarker: "token=raw-token-secret password=raw-password-secret Authorization: Bearer raw-bearer-secret");

        var decision = service.EvaluateAccess(request);

        Assert.NotNull(decision.AuthDecision);
        Assert.Contains(
            decision.AuthDecision!.AuditEvents,
            auditEvent => auditEvent.EventName == AdventuresAuthEventNames.SecretRedacted);
        Assert.Equal("true", decision.Metadata["SecretsRedacted"]);

        var evidenceText = string.Join(
            " ",
            decision.Metadata.Values
                .Concat(decision.AuthDecision.AuditEvents.SelectMany(auditEvent => auditEvent.Metadata.Values)));
        Assert.DoesNotContain("raw-token-secret", evidenceText, StringComparison.Ordinal);
        Assert.DoesNotContain("raw-password-secret", evidenceText, StringComparison.Ordinal);
        Assert.DoesNotContain("raw-bearer-secret", evidenceText, StringComparison.Ordinal);
        Assert.Contains("[REDACTED]", evidenceText, StringComparison.Ordinal);
    }

    [Fact]
    public void Consumer_uses_no_persistence_or_blogengine_dependency()
    {
        var service = new BlogAiAuthConsumerService();

        Assert.False(service.UsesPersistence);
        Assert.Equal("None", service.StorageKind);
        Assert.DoesNotContain(
            typeof(BlogAiAuthConsumerService).Assembly.GetReferencedAssemblies(),
            reference => reference.Name != null && reference.Name.Contains("BlogEngine", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Composition_registers_consumer_interface_boundary()
    {
        var serviceProvider = new ServiceCollection()
            .AddBlogAiAuthConsumerServices()
            .BuildServiceProvider();

        var service = serviceProvider.GetRequiredService<IBlogAiAuthConsumerService>();

        Assert.IsType<BlogAiAuthConsumerService>(service);
        var decision = service.EvaluateAccess(CreateRequest(devAuthMarker: "local-dev-credential"));
        Assert.True(decision.Allowed);
        Assert.Equal("DevCredentialAccepted", decision.ReasonCode);
    }

    private static BlogAiAuthConsumerRequest CreateRequest(
        string resourceName = "/blogAi/protected",
        bool requiresAuthentication = true,
        AdventuresAuthCorrelation? correlation = null,
        string? devAuthMarker = null,
        string? localSessionId = null)
    {
        return new BlogAiAuthConsumerRequest(
            resourceName,
            requiresAuthentication,
            correlation ?? new AdventuresAuthCorrelation(
                "corr-blogai-1",
                "request-blogai-1",
                "auth-blogai-1",
                "BlogAI",
                "LocalDevelopment"),
            devAuthMarker,
            localSessionId);
    }

    private static IBlogAiAuthConsumerService CreateService()
    {
        return new BlogAiAuthConsumerService();
    }

    private static void AssertCorrelation(
        AdventuresAuthCorrelation expected,
        AdventuresAuthCorrelation actual)
    {
        Assert.Equal(expected.CorrelationId, actual.CorrelationId);
        Assert.Equal(expected.RequestId, actual.RequestId);
        Assert.Equal(expected.AuthDecisionId, actual.AuthDecisionId);
        Assert.Equal(expected.ClientApplication, actual.ClientApplication);
        Assert.Equal(expected.Environment, actual.Environment);
    }
}
