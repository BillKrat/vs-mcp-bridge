using System.Collections.Generic;

namespace VsMcpBridge.Shared.AdventuresAuth
{
    public sealed class AdventuresAuthDecision
    {
        public AdventuresAuthDecision(
            bool allowed,
            string outcome,
            string reasonCode,
            AdventuresAuthCorrelation correlation,
            AdventuresAuthPrincipal? principal,
            string? localSessionId,
            IReadOnlyList<AdventuresAuthAuditEvent> auditEvents)
        {
            Allowed = allowed;
            Outcome = outcome;
            ReasonCode = reasonCode;
            Correlation = correlation;
            Principal = principal;
            LocalSessionId = localSessionId;
            AuditEvents = auditEvents;
        }

        public bool Allowed { get; }

        public string Outcome { get; }

        public string ReasonCode { get; }

        public AdventuresAuthCorrelation Correlation { get; }

        public AdventuresAuthPrincipal? Principal { get; }

        public string? LocalSessionId { get; }

        public IReadOnlyList<AdventuresAuthAuditEvent> AuditEvents { get; }
    }
}
