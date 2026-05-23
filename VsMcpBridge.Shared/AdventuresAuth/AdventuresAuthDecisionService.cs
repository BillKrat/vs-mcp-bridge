using System;
using System.Collections.Generic;
using VsMcpBridge.Shared.Security;

namespace VsMcpBridge.Shared.AdventuresAuth
{
    public sealed class AdventuresAuthDecisionService : IAdventuresAuthDecisionService
    {
        private const string ValidDevelopmentCredential = "local-dev-credential";
        private const string LocalSessionPrefix = "local-session-";

        private readonly object _gate = new object();
        private readonly HashSet<string> _activeSessions = new HashSet<string>(StringComparer.Ordinal);
        private readonly ISecurityRedactor _redactor;

        public AdventuresAuthDecisionService()
            : this(new BridgeSecurityRedactor())
        {
        }

        public AdventuresAuthDecisionService(ISecurityRedactor redactor)
        {
            _redactor = redactor ?? throw new ArgumentNullException(nameof(redactor));
        }

        public bool UsesPersistence => false;

        public string StorageKind => "LocalInMemoryOnly";

        public AdventuresAuthDecision Login(AdventuresAuthRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var events = CreateEventList(request, AdventuresAuthEventNames.RequestReceived);
            AddRedactionEventIfNeeded(events, request);

            var allowed = string.Equals(request.DevCredential, ValidDevelopmentCredential, StringComparison.Ordinal);
            var reasonCode = allowed ? "DevCredentialAccepted" : "DevCredentialRejected";
            var principal = allowed ? CreateLocalPrincipal() : null;
            var sessionId = allowed ? CreateSessionId(request.Correlation) : null;

            if (sessionId != null)
            {
                lock (_gate)
                    _activeSessions.Add(sessionId);
            }

            events.Add(CreateEvent(request, AdventuresAuthEventNames.LoginEvaluated, allowed, reasonCode));
            events.Add(CreateEvent(
                request,
                allowed ? AdventuresAuthEventNames.AccessAllowed : AdventuresAuthEventNames.AccessDenied,
                allowed,
                reasonCode));

            return new AdventuresAuthDecision(
                allowed,
                allowed ? "Allowed" : "Denied",
                reasonCode,
                request.Correlation,
                principal,
                sessionId,
                events);
        }

        public AdventuresAuthDecision ValidateSession(AdventuresAuthRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var events = CreateEventList(request, AdventuresAuthEventNames.RequestReceived);
            AddRedactionEventIfNeeded(events, request);

            var allowed = IsActiveSession(request.LocalSessionId);
            var reasonCode = allowed ? "LocalSessionAccepted" : "LocalSessionRejected";
            var principal = allowed ? CreateLocalPrincipal() : null;

            events.Add(CreateEvent(request, AdventuresAuthEventNames.SessionValidated, allowed, reasonCode));
            events.Add(CreateEvent(
                request,
                allowed ? AdventuresAuthEventNames.AccessAllowed : AdventuresAuthEventNames.AccessDenied,
                allowed,
                reasonCode));

            return new AdventuresAuthDecision(
                allowed,
                allowed ? "Allowed" : "Denied",
                reasonCode,
                request.Correlation,
                principal,
                allowed ? request.LocalSessionId : null,
                events);
        }

        public AdventuresAuthDecision Logout(AdventuresAuthRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var events = CreateEventList(request, AdventuresAuthEventNames.RequestReceived);
            AddRedactionEventIfNeeded(events, request);

            var invalidated = false;
            if (!string.IsNullOrWhiteSpace(request.LocalSessionId))
            {
                lock (_gate)
                    invalidated = _activeSessions.Remove(request.LocalSessionId!);
            }

            var reasonCode = invalidated ? "LocalSessionInvalidated" : "LocalSessionNotFound";
            events.Add(CreateEvent(request, AdventuresAuthEventNames.SessionValidated, invalidated, reasonCode));
            events.Add(CreateEvent(
                request,
                invalidated ? AdventuresAuthEventNames.AccessAllowed : AdventuresAuthEventNames.AccessDenied,
                invalidated,
                reasonCode));

            return new AdventuresAuthDecision(
                invalidated,
                invalidated ? "Allowed" : "Denied",
                reasonCode,
                request.Correlation,
                null,
                null,
                events);
        }

        public AdventuresAuthDecision CurrentPrincipal(AdventuresAuthRequest request)
        {
            var decision = ValidateSession(request);
            return new AdventuresAuthDecision(
                decision.Allowed,
                decision.Outcome,
                decision.Allowed ? "CurrentPrincipalReturned" : decision.ReasonCode,
                decision.Correlation,
                decision.Principal,
                decision.LocalSessionId,
                decision.AuditEvents);
        }

        private static AdventuresAuthPrincipal CreateLocalPrincipal()
        {
            return new AdventuresAuthPrincipal(
                "local-dev-user",
                "Local Development User",
                new Dictionary<string, string>
                {
                    ["authMode"] = "DevelopmentOnly",
                    ["clientApplication"] = "BlogAI"
                });
        }

        private static string CreateSessionId(AdventuresAuthCorrelation correlation)
        {
            return LocalSessionPrefix + correlation.AuthDecisionId;
        }

        private bool IsActiveSession(string? localSessionId)
        {
            if (string.IsNullOrWhiteSpace(localSessionId))
                return false;

            lock (_gate)
                return _activeSessions.Contains(localSessionId!);
        }

        private List<AdventuresAuthAuditEvent> CreateEventList(
            AdventuresAuthRequest request,
            string firstEventName)
        {
            return new List<AdventuresAuthAuditEvent>
            {
                CreateEvent(request, firstEventName, false, "Received")
            };
        }

        private void AddRedactionEventIfNeeded(
            ICollection<AdventuresAuthAuditEvent> events,
            AdventuresAuthRequest request)
        {
            var redacted = false;
            var metadata = new Dictionary<string, string>();

            AddRedactedValue(metadata, "credential", request.DevCredential, ref redacted);
            AddRedactedValue(metadata, "session", request.LocalSessionId, ref redacted);

            if (redacted)
                events.Add(CreateEvent(request, AdventuresAuthEventNames.SecretRedacted, false, "SecretRedacted", metadata));
        }

        private void AddRedactedValue(
            IDictionary<string, string> metadata,
            string name,
            string? value,
            ref bool redacted)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            var result = _redactor.Redact(value);
            if (!result.WasRedacted)
                return;

            metadata[name] = result.Value;
            redacted = true;
        }

        private static AdventuresAuthAuditEvent CreateEvent(
            AdventuresAuthRequest request,
            string eventName,
            bool allowed,
            string reasonCode,
            IReadOnlyDictionary<string, string>? metadata = null)
        {
            var eventMetadata = new Dictionary<string, string>
            {
                ["CorrelationId"] = request.Correlation.CorrelationId,
                ["RequestId"] = request.Correlation.RequestId,
                ["AuthDecisionId"] = request.Correlation.AuthDecisionId,
                ["ClientApplication"] = request.Correlation.ClientApplication,
                ["Environment"] = request.Correlation.Environment
            };

            if (metadata != null)
            {
                foreach (var item in metadata)
                    eventMetadata[item.Key] = item.Value;
            }

            return new AdventuresAuthAuditEvent(
                eventName,
                request.Correlation,
                allowed,
                GetEventOutcome(allowed, reasonCode),
                reasonCode,
                eventMetadata);
        }

        private static string GetEventOutcome(bool allowed, string reasonCode)
        {
            if (string.Equals(reasonCode, "Received", StringComparison.Ordinal))
                return "Received";

            if (string.Equals(reasonCode, "SecretRedacted", StringComparison.Ordinal))
                return "Redacted";

            return allowed ? "Allowed" : "Denied";
        }
    }
}
