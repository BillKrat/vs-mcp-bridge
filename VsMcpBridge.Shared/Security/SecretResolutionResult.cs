namespace VsMcpBridge.Shared.Security
{
    public sealed class SecretResolutionResult
    {
        private SecretResolutionResult(bool resolved, string reason)
        {
            Resolved = resolved;
            Reason = string.IsNullOrWhiteSpace(reason) ? "Unspecified" : reason;
        }

        public bool Resolved { get; }

        public string Reason { get; }

        public static SecretResolutionResult ResolvedReference(string reason = "Resolved")
            => new SecretResolutionResult(true, reason);

        public static SecretResolutionResult Unresolved(string reason)
            => new SecretResolutionResult(false, reason);
    }
}
