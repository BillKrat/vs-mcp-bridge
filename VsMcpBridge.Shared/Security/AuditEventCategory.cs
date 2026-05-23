namespace VsMcpBridge.Shared.Security
{
    public enum AuditEventCategory
    {
        Unknown = 0,
        ToolExecution = 1,
        Policy = 2,
        Approval = 3,
        Secret = 4,
        Execution = 5,
        DocumentPreview = 6,
        CodexHandoffPreview = 7
    }
}
