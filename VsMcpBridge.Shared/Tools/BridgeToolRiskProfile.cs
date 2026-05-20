using VsMcpBridge.Shared.Security;

namespace VsMcpBridge.Shared.Tools
{
    public sealed class BridgeToolRiskProfile
    {
        public AuditEventCategory AuditCategoryHint { get; set; } = AuditEventCategory.ToolExecution;

        public AuditSeverity SeverityHint { get; set; } = AuditSeverity.Informational;

        public AuditRiskLevel RiskLevelHint { get; set; } = AuditRiskLevel.Low;
    }
}
