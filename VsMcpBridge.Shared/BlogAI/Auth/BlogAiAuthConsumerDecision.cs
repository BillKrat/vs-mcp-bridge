using System.Collections.Generic;
using VsMcpBridge.Shared.AdventuresAuth;

namespace VsMcpBridge.Shared.BlogAI.Auth
{
    public sealed class BlogAiAuthConsumerDecision
    {
        public BlogAiAuthConsumerDecision(
            BlogAiProtectedResourceAccessDecision accessDecision,
            string outcome,
            string reasonCode,
            string resourceName,
            AdventuresAuthCorrelation correlation,
            AdventuresAuthDecision? authDecision,
            AdventuresAuthPrincipal? principal,
            IReadOnlyDictionary<string, string> metadata)
        {
            AccessDecision = accessDecision;
            Outcome = outcome;
            ReasonCode = reasonCode;
            ResourceName = resourceName;
            Correlation = correlation;
            AuthDecision = authDecision;
            Principal = principal;
            Metadata = metadata;
        }

        public bool Allowed => AccessDecision == BlogAiProtectedResourceAccessDecision.Allowed;

        public BlogAiProtectedResourceAccessDecision AccessDecision { get; }

        public string Outcome { get; }

        public string ReasonCode { get; }

        public string ResourceName { get; }

        public AdventuresAuthCorrelation Correlation { get; }

        public AdventuresAuthDecision? AuthDecision { get; }

        public AdventuresAuthPrincipal? Principal { get; }

        public IReadOnlyDictionary<string, string> Metadata { get; }
    }
}
