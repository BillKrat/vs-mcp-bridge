namespace VsMcpBridge.Shared.Security
{
    public sealed class AuditClassificationMetadata
    {
        public AuditClassificationMetadata()
        {
        }

        public AuditClassificationMetadata(
            AuditEventCategory category,
            AuditSeverity severity,
            AuditRiskLevel riskLevel,
            AuditOutcome outcome)
        {
            Category = category;
            Severity = severity;
            RiskLevel = riskLevel;
            Outcome = outcome;
        }

        public AuditEventCategory Category { get; set; } = AuditEventCategory.ToolExecution;

        public AuditSeverity Severity { get; set; } = AuditSeverity.Informational;

        public AuditRiskLevel RiskLevel { get; set; } = AuditRiskLevel.Low;

        public AuditOutcome Outcome { get; set; } = AuditOutcome.Unknown;
    }
}
