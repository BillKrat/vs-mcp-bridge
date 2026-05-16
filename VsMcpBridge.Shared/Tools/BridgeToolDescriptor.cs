namespace VsMcpBridge.Shared.Tools
{
    using VsMcpBridge.Shared.Security;

    public sealed class BridgeToolDescriptor
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Host { get; set; } = string.Empty;
        public ToolExecutionApprovalRequirement ApprovalRequirement { get; set; } = ToolExecutionApprovalRequirement.NotRequired;
        public bool ApprovalRequired
        {
            get => ApprovalRequirement == ToolExecutionApprovalRequirement.Required;
            set => ApprovalRequirement = value ? ToolExecutionApprovalRequirement.Required : ToolExecutionApprovalRequirement.NotRequired;
        }
    }
}
