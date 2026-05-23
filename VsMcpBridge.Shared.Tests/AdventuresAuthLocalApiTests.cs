using System;
using System.Linq;
using System.Text.Json;
using Adventures.Auth.LocalApi;
using Microsoft.AspNetCore.Http.HttpResults;
using VsMcpBridge.Shared.AdventuresAuth;
using Xunit;

namespace VsMcpBridge.Shared.Tests;

public sealed class AdventuresAuthLocalApiTests
{
    [Fact]
    public void Login_without_credential_denies_access()
    {
        var service = CreateService();
        var response = service.Login(CreateRequest());

        Assert.False(response.Allowed);
        Assert.Equal("Denied", response.Outcome);
        Assert.Equal("DevCredentialRejected", response.ReasonCode);
        Assert.Null(response.Principal);
        Assert.Null(response.LocalSessionId);
        Assert.Contains(AdventuresAuthEventNames.AccessDenied, response.AuditEventNames);
        AssertCorrelation(response);
    }

    [Fact]
    public void Login_with_valid_development_credential_allows_access()
    {
        var service = CreateService();
        var response = service.Login(CreateRequest(devCredential: "local-dev-credential"));

        Assert.True(response.Allowed);
        Assert.Equal("Allowed", response.Outcome);
        Assert.Equal("DevCredentialAccepted", response.ReasonCode);
        Assert.NotNull(response.Principal);
        Assert.Equal("local-dev-user", response.Principal!.Subject);
        Assert.Equal("DevelopmentOnly", response.Principal.Claims["authMode"]);
        Assert.NotNull(response.LocalSessionId);
        Assert.StartsWith("local-session-", response.LocalSessionId, StringComparison.Ordinal);
        Assert.Contains(AdventuresAuthEventNames.AccessAllowed, response.AuditEventNames);
    }

    [Fact]
    public void Invalid_credential_and_invalid_session_are_denied()
    {
        var service = CreateService();

        var invalidCredential = service.Login(CreateRequest(devCredential: "invalid-local-credential"));
        var invalidSession = service.Validate(CreateRequest(localSessionId: "invalid-session-token"));

        Assert.False(invalidCredential.Allowed);
        Assert.Equal("DevCredentialRejected", invalidCredential.ReasonCode);
        Assert.False(invalidSession.Allowed);
        Assert.Equal("LocalSessionRejected", invalidSession.ReasonCode);
    }

    [Fact]
    public void Current_principal_returns_placeholder_for_valid_session()
    {
        var service = CreateService();
        var login = service.Login(CreateRequest(devCredential: "local-dev-credential"));

        var currentPrincipal = service.CurrentPrincipal(CreateRequest(
            authDecisionId: "auth-me",
            localSessionId: login.LocalSessionId));

        Assert.True(currentPrincipal.Allowed);
        Assert.Equal("CurrentPrincipalReturned", currentPrincipal.ReasonCode);
        Assert.NotNull(currentPrincipal.Principal);
        Assert.Equal("local-dev-user", currentPrincipal.Principal!.Subject);
    }

    [Fact]
    public void Logout_invalidates_local_session_placeholder()
    {
        var service = CreateService();
        var login = service.Login(CreateRequest(devCredential: "local-dev-credential"));

        var logout = service.Logout(CreateRequest(
            authDecisionId: "auth-logout",
            localSessionId: login.LocalSessionId));
        var validateAfterLogout = service.Validate(CreateRequest(
            authDecisionId: "auth-after-logout",
            localSessionId: login.LocalSessionId));

        Assert.True(logout.Allowed);
        Assert.Equal("LocalSessionInvalidated", logout.ReasonCode);
        Assert.False(validateAfterLogout.Allowed);
        Assert.Equal("LocalSessionRejected", validateAfterLogout.ReasonCode);
    }

    [Fact]
    public void Supplied_correlation_is_preserved_and_missing_correlation_is_generated()
    {
        var service = CreateService();

        var supplied = service.Login(CreateRequest(
            correlationId: "corr-supplied",
            requestId: "request-supplied",
            authDecisionId: "auth-supplied",
            clientApplication: "BlogAI",
            environment: "LocalDevelopment",
            devCredential: "local-dev-credential"));
        var generated = service.Login(new AdventuresAuthApiRequest
        {
            DevCredential = "local-dev-credential"
        });

        Assert.Equal("corr-supplied", supplied.CorrelationId);
        Assert.Equal("request-supplied", supplied.RequestId);
        Assert.Equal("auth-supplied", supplied.AuthDecisionId);
        Assert.Equal("BlogAI", supplied.ClientApplication);
        Assert.Equal("LocalDevelopment", supplied.Environment);
        Assert.StartsWith("corr-", generated.CorrelationId, StringComparison.Ordinal);
        Assert.StartsWith("request-", generated.RequestId, StringComparison.Ordinal);
        Assert.StartsWith("auth-", generated.AuthDecisionId, StringComparison.Ordinal);
        Assert.Equal("BlogAI", generated.ClientApplication);
        Assert.Equal("LocalDevelopment", generated.Environment);
    }

    [Fact]
    public void Secret_like_values_are_redacted_from_response_evidence()
    {
        var service = CreateService();
        var response = service.Login(CreateRequest(
            devCredential: "token=raw-token-secret password=raw-password-secret Authorization: Bearer raw-bearer-secret"));

        var evidenceText = JsonSerializer.Serialize(response);

        Assert.False(response.Allowed);
        Assert.True(response.SecretsRedacted);
        Assert.Contains(AdventuresAuthEventNames.SecretRedacted, response.AuditEventNames);
        Assert.DoesNotContain("raw-token-secret", evidenceText, StringComparison.Ordinal);
        Assert.DoesNotContain("raw-password-secret", evidenceText, StringComparison.Ordinal);
        Assert.DoesNotContain("raw-bearer-secret", evidenceText, StringComparison.Ordinal);
    }

    [Fact]
    public void Endpoint_handler_delegates_login_to_api_service()
    {
        var fakeService = new RecordingApiService();
        var request = CreateRequest(devCredential: "local-dev-credential");

        var result = AdventuresAuthEndpointHandlers.Login(request, fakeService);

        var ok = Assert.IsType<Ok<AdventuresAuthApiResponse>>(result);
        Assert.Equal("Login", fakeService.LastOperation);
        Assert.Same(fakeService.Response, ok.Value);
    }

    [Fact]
    public void Local_api_service_uses_no_persistence_or_blogengine_dependency()
    {
        var service = CreateService();

        Assert.False(service.UsesPersistence);
        Assert.Equal("None", service.StorageKind);
        Assert.DoesNotContain(
            typeof(AdventuresAuthApiService).Assembly.GetReferencedAssemblies(),
            reference => reference.Name != null && reference.Name.Contains("BlogEngine", StringComparison.OrdinalIgnoreCase));
    }

    private static AdventuresAuthApiService CreateService()
    {
        return new AdventuresAuthApiService(new AdventuresAuthDecisionService());
    }

    private static AdventuresAuthApiRequest CreateRequest(
        string correlationId = "corr-api-1",
        string requestId = "request-api-1",
        string authDecisionId = "auth-api-1",
        string clientApplication = "BlogAI",
        string environment = "LocalDevelopment",
        string? devCredential = null,
        string? localSessionId = null)
    {
        return new AdventuresAuthApiRequest
        {
            CorrelationId = correlationId,
            RequestId = requestId,
            AuthDecisionId = authDecisionId,
            ClientApplication = clientApplication,
            Environment = environment,
            DevCredential = devCredential,
            LocalSessionId = localSessionId
        };
    }

    private static void AssertCorrelation(AdventuresAuthApiResponse response)
    {
        Assert.Equal("corr-api-1", response.CorrelationId);
        Assert.Equal("request-api-1", response.RequestId);
        Assert.Equal("auth-api-1", response.AuthDecisionId);
        Assert.Equal("BlogAI", response.ClientApplication);
        Assert.Equal("LocalDevelopment", response.Environment);
    }

    private sealed class RecordingApiService : IAdventuresAuthApiService
    {
        public AdventuresAuthApiResponse Response { get; } = new AdventuresAuthApiResponse
        {
            Allowed = true,
            Outcome = "Allowed",
            ReasonCode = "Recorded"
        };

        public string? LastOperation { get; private set; }

        public bool UsesPersistence => false;

        public string StorageKind => "None";

        public AdventuresAuthApiResponse Login(AdventuresAuthApiRequest request)
        {
            LastOperation = "Login";
            return Response;
        }

        public AdventuresAuthApiResponse Logout(AdventuresAuthApiRequest request)
        {
            LastOperation = "Logout";
            return Response;
        }

        public AdventuresAuthApiResponse CurrentPrincipal(AdventuresAuthApiRequest request)
        {
            LastOperation = "CurrentPrincipal";
            return Response;
        }

        public AdventuresAuthApiResponse Validate(AdventuresAuthApiRequest request)
        {
            LastOperation = "Validate";
            return Response;
        }
    }
}
