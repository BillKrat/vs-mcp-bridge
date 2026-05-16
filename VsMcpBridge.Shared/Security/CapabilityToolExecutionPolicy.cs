using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace VsMcpBridge.Shared.Security
{
    public sealed class CapabilityToolExecutionPolicy : IToolExecutionPolicy
    {
        private readonly CapabilityToolExecutionPolicyOptions _options;

        public CapabilityToolExecutionPolicy()
            : this(new CapabilityToolExecutionPolicyOptions())
        {
        }

        public CapabilityToolExecutionPolicy(CapabilityToolExecutionPolicyOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public Task<ToolExecutionPolicyDecision> EvaluateAsync(ToolExecutionSecurityContext context, CancellationToken cancellationToken)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var requiredCapabilities = GetCapabilityNames(context.RequiredCapabilities);
            if (requiredCapabilities.Count == 0)
                return Task.FromResult(ToolExecutionPolicyDecision.Allow("No required capabilities"));

            var deniedCapability = requiredCapabilities.FirstOrDefault(capability => _options.DeniedCapabilities.Contains(capability));
            if (!string.IsNullOrEmpty(deniedCapability))
                return Task.FromResult(ToolExecutionPolicyDecision.Deny($"Denied required capability '{deniedCapability}'."));

            if (_options.DenyUnknownRequiredCapabilities)
            {
                var unknownCapability = requiredCapabilities.FirstOrDefault(capability => !_options.AllowedCapabilities.Contains(capability));
                if (!string.IsNullOrEmpty(unknownCapability))
                    return Task.FromResult(ToolExecutionPolicyDecision.Deny($"Unknown required capability '{unknownCapability}'."));
            }

            return Task.FromResult(ToolExecutionPolicyDecision.Allow("Required capabilities allowed"));
        }

        private static IReadOnlyList<string> GetCapabilityNames(IReadOnlyList<BridgeCapability> capabilities)
        {
            if (capabilities == null || capabilities.Count == 0)
                return Array.Empty<string>();

            return capabilities
                .Where(capability => capability != null && !string.IsNullOrWhiteSpace(capability.Name))
                .Select(capability => capability.Name)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }
    }
}
