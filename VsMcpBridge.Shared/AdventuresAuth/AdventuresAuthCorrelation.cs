using System;

namespace VsMcpBridge.Shared.AdventuresAuth
{
    public sealed class AdventuresAuthCorrelation
    {
        public AdventuresAuthCorrelation(
            string correlationId,
            string requestId,
            string authDecisionId,
            string clientApplication,
            string environment)
        {
            CorrelationId = RequireValue(correlationId, nameof(correlationId));
            RequestId = RequireValue(requestId, nameof(requestId));
            AuthDecisionId = RequireValue(authDecisionId, nameof(authDecisionId));
            ClientApplication = RequireValue(clientApplication, nameof(clientApplication));
            Environment = RequireValue(environment, nameof(environment));
        }

        public string CorrelationId { get; }

        public string RequestId { get; }

        public string AuthDecisionId { get; }

        public string ClientApplication { get; }

        public string Environment { get; }

        public static AdventuresAuthCorrelation Create(
            string clientApplication = "BlogAI",
            string environment = "LocalDevelopment")
        {
            return new AdventuresAuthCorrelation(
                Guid.NewGuid().ToString("N"),
                Guid.NewGuid().ToString("N"),
                Guid.NewGuid().ToString("N"),
                clientApplication,
                environment);
        }

        private static string RequireValue(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("A correlation value is required.", parameterName);

            return value;
        }
    }
}
