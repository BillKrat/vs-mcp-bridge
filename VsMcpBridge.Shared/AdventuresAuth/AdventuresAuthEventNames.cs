namespace VsMcpBridge.Shared.AdventuresAuth
{
    public static class AdventuresAuthEventNames
    {
        public const string RequestReceived = "AdventuresAuth.RequestReceived";
        public const string LoginEvaluated = "AdventuresAuth.LoginEvaluated";
        public const string SessionValidated = "AdventuresAuth.SessionValidated";
        public const string AccessAllowed = "AdventuresAuth.AccessAllowed";
        public const string AccessDenied = "AdventuresAuth.AccessDenied";
        public const string SecretRedacted = "AdventuresAuth.SecretRedacted";
    }
}
