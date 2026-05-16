namespace VsMcpBridge.Shared.Security
{
    public sealed class ToolExecutionApprovalDecision
    {
        private ToolExecutionApprovalDecision(bool approved, string reason)
        {
            Approved = approved;
            Reason = reason;
        }

        public bool Approved { get; }

        public string Reason { get; }

        public static ToolExecutionApprovalDecision Approve(string reason = "Approved")
            => new ToolExecutionApprovalDecision(true, string.IsNullOrWhiteSpace(reason) ? "Approved" : reason);

        public static ToolExecutionApprovalDecision Deny(string reason)
            => new ToolExecutionApprovalDecision(false, string.IsNullOrWhiteSpace(reason) ? "Denied" : reason);
    }
}
