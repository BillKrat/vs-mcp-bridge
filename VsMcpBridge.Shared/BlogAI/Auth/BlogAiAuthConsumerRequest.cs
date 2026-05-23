using System;
using VsMcpBridge.Shared.AdventuresAuth;

namespace VsMcpBridge.Shared.BlogAI.Auth
{
    public sealed class BlogAiAuthConsumerRequest
    {
        public BlogAiAuthConsumerRequest(
            string resourceName,
            bool requiresAuthentication,
            AdventuresAuthCorrelation correlation,
            string? devAuthMarker = null,
            string? localSessionId = null)
        {
            if (string.IsNullOrWhiteSpace(resourceName))
                throw new ArgumentException("Resource name is required.", nameof(resourceName));

            ResourceName = resourceName;
            RequiresAuthentication = requiresAuthentication;
            Correlation = correlation ?? throw new ArgumentNullException(nameof(correlation));
            DevAuthMarker = devAuthMarker;
            LocalSessionId = localSessionId;
        }

        public string ResourceName { get; }

        public bool RequiresAuthentication { get; }

        public AdventuresAuthCorrelation Correlation { get; }

        public string? DevAuthMarker { get; }

        public string? LocalSessionId { get; }
    }
}
