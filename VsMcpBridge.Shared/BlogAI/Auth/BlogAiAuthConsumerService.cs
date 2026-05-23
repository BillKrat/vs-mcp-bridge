using System;
using System.Collections.Generic;
using System.Linq;
using VsMcpBridge.Shared.AdventuresAuth;

namespace VsMcpBridge.Shared.BlogAI.Auth
{
    public sealed class BlogAiAuthConsumerService : IBlogAiAuthConsumerService
    {
        private readonly AdventuresAuthDecisionService _authDecisionService;

        public BlogAiAuthConsumerService()
            : this(new AdventuresAuthDecisionService())
        {
        }

        public BlogAiAuthConsumerService(AdventuresAuthDecisionService authDecisionService)
        {
            _authDecisionService = authDecisionService ?? throw new ArgumentNullException(nameof(authDecisionService));
        }

        public bool UsesPersistence => false;

        public string StorageKind => "None";

        public BlogAiAuthConsumerDecision EvaluateAccess(BlogAiAuthConsumerRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (!request.RequiresAuthentication)
                return CreatePublicDecision(request);

            var authRequest = new AdventuresAuthRequest(
                request.Correlation,
                request.DevAuthMarker,
                request.LocalSessionId);

            var authDecision = string.IsNullOrWhiteSpace(request.DevAuthMarker)
                ? _authDecisionService.ValidateSession(authRequest)
                : _authDecisionService.Login(authRequest);

            var accessDecision = authDecision.Allowed
                ? BlogAiProtectedResourceAccessDecision.Allowed
                : BlogAiProtectedResourceAccessDecision.Denied;

            var metadata = CreateMetadata(
                request,
                accessDecision,
                authDecision.ReasonCode,
                authDecision);

            return new BlogAiAuthConsumerDecision(
                accessDecision,
                authDecision.Allowed ? "Allowed" : "Denied",
                authDecision.ReasonCode,
                request.ResourceName,
                authDecision.Correlation,
                authDecision,
                authDecision.Principal,
                metadata);
        }

        private static BlogAiAuthConsumerDecision CreatePublicDecision(BlogAiAuthConsumerRequest request)
        {
            var metadata = CreateMetadata(
                request,
                BlogAiProtectedResourceAccessDecision.Allowed,
                "PublicResource",
                null);

            return new BlogAiAuthConsumerDecision(
                BlogAiProtectedResourceAccessDecision.Allowed,
                "Allowed",
                "PublicResource",
                request.ResourceName,
                request.Correlation,
                null,
                null,
                metadata);
        }

        private static IReadOnlyDictionary<string, string> CreateMetadata(
            BlogAiAuthConsumerRequest request,
            BlogAiProtectedResourceAccessDecision accessDecision,
            string reasonCode,
            AdventuresAuthDecision? authDecision)
        {
            var metadata = new Dictionary<string, string>
            {
                ["ResourceName"] = request.ResourceName,
                ["ResourceCategory"] = request.RequiresAuthentication ? "Protected" : "Public",
                ["Outcome"] = accessDecision == BlogAiProtectedResourceAccessDecision.Allowed ? "Allowed" : "Denied",
                ["ReasonCode"] = reasonCode,
                ["CorrelationId"] = request.Correlation.CorrelationId,
                ["RequestId"] = request.Correlation.RequestId,
                ["AuthDecisionId"] = request.Correlation.AuthDecisionId,
                ["ClientApplication"] = request.Correlation.ClientApplication,
                ["Environment"] = request.Correlation.Environment,
                ["AuthenticationBoundary"] = "AdventuresAuth",
                ["StorageKind"] = "None"
            };

            if (authDecision != null)
            {
                metadata["AuthEventNames"] = string.Join(
                    ",",
                    authDecision.AuditEvents.Select(auditEvent => auditEvent.EventName));
                metadata["SecretsRedacted"] = authDecision.AuditEvents.Any(
                    auditEvent => auditEvent.EventName == AdventuresAuthEventNames.SecretRedacted)
                    ? "true"
                    : "false";
            }
            else
            {
                metadata["AuthEventNames"] = string.Empty;
                metadata["SecretsRedacted"] = "false";
            }

            return metadata;
        }
    }
}
