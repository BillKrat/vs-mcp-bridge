namespace VsMcpBridge.Shared.AdventuresAuth
{
    public sealed class AdventuresAuthRequest
    {
        public AdventuresAuthRequest(
            AdventuresAuthCorrelation correlation,
            string? devCredential = null,
            string? localSessionId = null)
        {
            Correlation = correlation;
            DevCredential = devCredential;
            LocalSessionId = localSessionId;
        }

        public AdventuresAuthCorrelation Correlation { get; }

        public string? DevCredential { get; }

        public string? LocalSessionId { get; }
    }
}
