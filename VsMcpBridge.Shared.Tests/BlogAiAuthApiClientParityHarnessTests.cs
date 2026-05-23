using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Adventures.Auth.LocalApi;
using Microsoft.AspNetCore.Http;
using BlogAI.Web.Auth;
using Microsoft.AspNetCore.Http.HttpResults;
using VsMcpBridge.Shared.AdventuresAuth;
using VsMcpBridge.Shared.BlogAI.Auth;
using Xunit;

namespace VsMcpBridge.Shared.Tests;

public sealed class BlogAiAuthApiClientParityHarnessTests
{
    [Fact]
    public async Task Api_client_diagnostic_path_matches_in_process_placeholder_decisions()
    {
        var service = new BlogAiLocalAuthStatusService(
            new BlogAiAuthConsumerService(),
            new LocalApiHarnessClient());

        var baseline = await service.GetStatusAsync();
        var apiDiagnostic = await service.GetStatusAsync(BlogAiLocalAuthStatusService.ApiClientAuthPath);

        Assert.Equal(BlogAiLocalAuthStatusService.InProcessAuthPath, baseline.AuthPath);
        Assert.False(baseline.IsDiagnosticMode);
        Assert.Equal(BlogAiLocalAuthStatusService.ApiClientAuthPath, apiDiagnostic.AuthPath);
        Assert.True(apiDiagnostic.IsDiagnosticMode);
        Assert.Contains("does not fall back", apiDiagnostic.DiagnosticMessage, StringComparison.Ordinal);

        AssertPlaceholderParity(baseline.Decisions[0], apiDiagnostic.Decisions[0]);
        AssertPlaceholderParity(baseline.Decisions[1], apiDiagnostic.Decisions[1]);
        Assert.False(baseline.Decisions[0].ProtectedPlaceholderVisible);
        Assert.True(baseline.Decisions[1].ProtectedPlaceholderVisible);
        Assert.False(apiDiagnostic.Decisions[0].ProtectedPlaceholderVisible);
        Assert.True(apiDiagnostic.Decisions[1].ProtectedPlaceholderVisible);
        AssertCorrelationPresent(apiDiagnostic.Decisions[0]);
        AssertCorrelationPresent(apiDiagnostic.Decisions[1]);
    }

    [Fact]
    public async Task Api_client_diagnostic_status_does_not_expose_secret_like_values()
    {
        var service = new BlogAiLocalAuthStatusService(
            new BlogAiAuthConsumerService(),
            new LocalApiHarnessClient());

        var apiDiagnostic = await service.GetStatusAsync(BlogAiLocalAuthStatusService.ApiClientAuthPath);
        var evidence = JsonSerializer.Serialize(apiDiagnostic);

        Assert.DoesNotContain("local-dev-credential", evidence, StringComparison.Ordinal);
        Assert.DoesNotContain("raw-token", evidence, StringComparison.Ordinal);
        Assert.DoesNotContain("raw-secret", evidence, StringComparison.Ordinal);
        Assert.DoesNotContain("Authorization:", evidence, StringComparison.Ordinal);
        Assert.DoesNotContain("Bearer ", evidence, StringComparison.Ordinal);
        Assert.DoesNotContain("password", evidence, StringComparison.OrdinalIgnoreCase);
    }

    private static void AssertPlaceholderParity(
        BlogAiLocalAuthDecisionDisplay baseline,
        BlogAiLocalAuthDecisionDisplay apiDiagnostic)
    {
        Assert.Equal(baseline.Allowed, apiDiagnostic.Allowed);
        Assert.Equal(baseline.ProtectedPlaceholderState, apiDiagnostic.ProtectedPlaceholderState);
        Assert.Equal(baseline.ProtectedPlaceholderVisible, apiDiagnostic.ProtectedPlaceholderVisible);
        Assert.Equal(baseline.ClientApplication, apiDiagnostic.ClientApplication);
        Assert.Equal(baseline.Environment, apiDiagnostic.Environment);
    }

    private static void AssertCorrelationPresent(BlogAiLocalAuthDecisionDisplay decision)
    {
        Assert.False(string.IsNullOrWhiteSpace(decision.CorrelationId));
        Assert.False(string.IsNullOrWhiteSpace(decision.RequestId));
        Assert.False(string.IsNullOrWhiteSpace(decision.AuthDecisionId));
        Assert.Equal("BlogAI", decision.ClientApplication);
        Assert.Equal("LocalDevelopment", decision.Environment);
    }

    private sealed class LocalApiHarnessClient : IBlogAiLocalAuthApiClient
    {
        private readonly AdventuresAuthApiService _service = new(new AdventuresAuthDecisionService());

        public Task<BlogAiLocalAuthApiClientDecision> LoginAsync(
            BlogAiLocalAuthApiClientRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = AdventuresAuthEndpointHandlers.Login(ToApiRequest(request), _service);
            return Task.FromResult(ToClientDecision(GetValue(result)));
        }

        public Task<BlogAiLocalAuthApiClientDecision> LogoutAsync(
            BlogAiLocalAuthApiClientRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = AdventuresAuthEndpointHandlers.Logout(ToApiRequest(request), _service);
            return Task.FromResult(ToClientDecision(GetValue(result)));
        }

        public Task<BlogAiLocalAuthApiClientDecision> GetCurrentPrincipalAsync(
            BlogAiLocalAuthApiClientRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = AdventuresAuthEndpointHandlers.CurrentPrincipal(
                request.CorrelationId,
                request.RequestId,
                request.AuthDecisionId,
                request.ClientApplication,
                request.Environment,
                request.LocalSessionId,
                _service);

            return Task.FromResult(ToClientDecision(GetValue(result)));
        }

        public Task<BlogAiLocalAuthApiClientDecision> ValidateAsync(
            BlogAiLocalAuthApiClientRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = AdventuresAuthEndpointHandlers.Validate(ToApiRequest(request), _service);
            return Task.FromResult(ToClientDecision(GetValue(result)));
        }

        private static AdventuresAuthApiRequest ToApiRequest(BlogAiLocalAuthApiClientRequest request)
        {
            return new AdventuresAuthApiRequest
            {
                CorrelationId = request.CorrelationId,
                RequestId = request.RequestId,
                AuthDecisionId = request.AuthDecisionId,
                ClientApplication = request.ClientApplication,
                Environment = request.Environment,
                DevCredential = request.DevCredential,
                LocalSessionId = request.LocalSessionId
            };
        }

        private static AdventuresAuthApiResponse GetValue(IResult result)
        {
            return Assert.IsType<Ok<AdventuresAuthApiResponse>>(result).Value!;
        }

        private static BlogAiLocalAuthApiClientDecision ToClientDecision(AdventuresAuthApiResponse response)
        {
            return new BlogAiLocalAuthApiClientDecision
            {
                Allowed = response.Allowed,
                Outcome = response.Outcome,
                ReasonCode = response.ReasonCode,
                CorrelationId = response.CorrelationId,
                RequestId = response.RequestId,
                AuthDecisionId = response.AuthDecisionId,
                ClientApplication = response.ClientApplication,
                Environment = response.Environment,
                Principal = response.Principal == null
                    ? null
                    : new BlogAiLocalAuthApiClientPrincipal
                    {
                        Subject = response.Principal.Subject,
                        DisplayName = response.Principal.DisplayName,
                        Claims = response.Principal.Claims
                    },
                LocalSessionId = response.LocalSessionId,
                AuditEventNames = response.AuditEventNames,
                SecretsRedacted = response.SecretsRedacted,
                StorageKind = response.StorageKind
            };
        }
    }
}
