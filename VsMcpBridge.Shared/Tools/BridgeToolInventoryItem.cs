using System;
using System.Collections.Generic;
using VsMcpBridge.Shared.Security;

namespace VsMcpBridge.Shared.Tools
{
    public sealed class BridgeToolInventoryItem
    {
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Version { get; set; } = BridgeToolManifest.DefaultVersion;

        public string Description { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public BridgeToolDiscoveryKind DiscoveryKind { get; set; } = BridgeToolDiscoveryKind.Unspecified;

        public string Source { get; set; } = string.Empty;

        public string HostAffinity { get; set; } = string.Empty;

        public bool IsHostSpecific { get; set; }

        public IReadOnlyList<string> RequiredCapabilities { get; set; } = Array.Empty<string>();

        public ToolExecutionApprovalRequirement ApprovalRequirement { get; set; } = ToolExecutionApprovalRequirement.NotRequired;

        public AuditEventCategory AuditCategoryHint { get; set; } = AuditEventCategory.ToolExecution;

        public AuditSeverity SeverityHint { get; set; } = AuditSeverity.Informational;

        public AuditRiskLevel RiskLevelHint { get; set; } = AuditRiskLevel.Low;

        public bool ExecutesThroughBridgeToolExecutor { get; set; } = true;
    }
}
