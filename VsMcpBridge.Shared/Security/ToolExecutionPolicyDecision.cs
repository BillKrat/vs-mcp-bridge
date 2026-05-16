namespace VsMcpBridge.Shared.Security
{
    public sealed class ToolExecutionPolicyDecision
    {
        private ToolExecutionPolicyDecision(bool allowed, string reason)
        {
            Allowed = allowed;
            Reason = reason;
        }

        public bool Allowed { get; }

        public string Reason { get; }

        public static ToolExecutionPolicyDecision Allow(string reason = "Allowed")
            => new ToolExecutionPolicyDecision(true, reason);

        public static ToolExecutionPolicyDecision Deny(string reason)
            => new ToolExecutionPolicyDecision(false, string.IsNullOrWhiteSpace(reason) ? "Denied" : reason);
    }
}
