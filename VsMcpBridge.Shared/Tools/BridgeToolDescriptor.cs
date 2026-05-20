namespace VsMcpBridge.Shared.Tools
{
    using System;
    using System.Collections.Generic;
    using VsMcpBridge.Shared.Security;

    public sealed class BridgeToolDescriptor
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = BridgeToolManifest.DefaultVersion;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Host { get; set; } = string.Empty;
        public BridgeToolDiscoveryKind DiscoveryKind { get; set; } = BridgeToolDiscoveryKind.Unspecified;
        public BridgeToolExecutionCharacteristics? ExecutionCharacteristics { get; set; }
        public BridgeToolRiskProfile? RiskProfile { get; set; }
        public BridgeToolHostAffinity? HostAffinity { get; set; }
        public IReadOnlyList<BridgeCapability> RequiredCapabilities { get; set; } = Array.Empty<BridgeCapability>();
        public ToolExecutionApprovalRequirement ApprovalRequirement { get; set; } = ToolExecutionApprovalRequirement.NotRequired;
        public BridgeToolManifest Manifest => BridgeToolManifest.FromDescriptor(this);
        public bool ApprovalRequired
        {
            get => ApprovalRequirement == ToolExecutionApprovalRequirement.Required;
            set => ApprovalRequirement = value ? ToolExecutionApprovalRequirement.Required : ToolExecutionApprovalRequirement.NotRequired;
        }
    }
}
