using System;
using System.Collections.Generic;
using VsMcpBridge.Shared.Security;

namespace VsMcpBridge.Shared.Tools
{
    public sealed class BridgeToolManifest
    {
        public const string DefaultVersion = "1.0.0";

        public BridgeToolIdentity Identity { get; set; } = new BridgeToolIdentity();

        public string Description { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public BridgeToolExecutionCharacteristics Execution { get; set; } = new BridgeToolExecutionCharacteristics();

        public IReadOnlyList<BridgeCapability> RequiredCapabilities { get; set; } = Array.Empty<BridgeCapability>();

        public ToolExecutionApprovalRequirement ApprovalRequirement { get; set; } = ToolExecutionApprovalRequirement.NotRequired;

        public BridgeToolRiskProfile RiskProfile { get; set; } = new BridgeToolRiskProfile();

        public BridgeToolHostAffinity HostAffinity { get; set; } = new BridgeToolHostAffinity();

        public static BridgeToolManifest FromDescriptor(BridgeToolDescriptor descriptor)
        {
            if (descriptor == null)
                throw new ArgumentNullException(nameof(descriptor));

            var discoveryKind = descriptor.DiscoveryKind == BridgeToolDiscoveryKind.Unspecified
                ? InferDiscoveryKind(descriptor.Source)
                : descriptor.DiscoveryKind;
            return new BridgeToolManifest
            {
                Identity = new BridgeToolIdentity
                {
                    Id = descriptor.Id,
                    Name = descriptor.Name,
                    Version = string.IsNullOrWhiteSpace(descriptor.Version) ? DefaultVersion : descriptor.Version
                },
                Description = descriptor.Description,
                Category = descriptor.Category,
                Execution = new BridgeToolExecutionCharacteristics
                {
                    Source = descriptor.ExecutionCharacteristics?.Source ?? descriptor.Source,
                    DiscoveryKind = descriptor.ExecutionCharacteristics?.DiscoveryKind == BridgeToolDiscoveryKind.Unspecified
                        ? discoveryKind
                        : descriptor.ExecutionCharacteristics?.DiscoveryKind ?? discoveryKind,
                    ExecutesThroughBridgeToolExecutor = descriptor.ExecutionCharacteristics?.ExecutesThroughBridgeToolExecutor ?? true
                },
                RequiredCapabilities = descriptor.RequiredCapabilities ?? Array.Empty<BridgeCapability>(),
                ApprovalRequirement = descriptor.ApprovalRequirement,
                RiskProfile = descriptor.RiskProfile ?? new BridgeToolRiskProfile(),
                HostAffinity = descriptor.HostAffinity ?? new BridgeToolHostAffinity { Host = descriptor.Host }
            };
        }

        private static BridgeToolDiscoveryKind InferDiscoveryKind(string source)
        {
            if (string.Equals(source, "Compiled", StringComparison.OrdinalIgnoreCase))
                return BridgeToolDiscoveryKind.Compiled;
            if (string.Equals(source, "MEF", StringComparison.OrdinalIgnoreCase))
                return BridgeToolDiscoveryKind.Mef;

            return BridgeToolDiscoveryKind.Unspecified;
        }
    }
}
