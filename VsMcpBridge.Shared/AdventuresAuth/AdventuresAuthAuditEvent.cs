using System.Collections.Generic;

namespace VsMcpBridge.Shared.AdventuresAuth
{
    public sealed class AdventuresAuthAuditEvent
    {
        public AdventuresAuthAuditEvent(
            string eventName,
            AdventuresAuthCorrelation correlation,
            bool allowed,
            string outcome,
            string reasonCode,
            IReadOnlyDictionary<string, string>? metadata = null)
        {
            EventName = eventName;
            Correlation = correlation;
            Allowed = allowed;
            Outcome = outcome;
            ReasonCode = reasonCode;
            Metadata = metadata ?? new Dictionary<string, string>();
        }

        public string EventName { get; }

        public AdventuresAuthCorrelation Correlation { get; }

        public bool Allowed { get; }

        public string Outcome { get; }

        public string ReasonCode { get; }

        public IReadOnlyDictionary<string, string> Metadata { get; }
    }
}
