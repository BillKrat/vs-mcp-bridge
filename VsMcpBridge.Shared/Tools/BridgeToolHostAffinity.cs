namespace VsMcpBridge.Shared.Tools
{
    public sealed class BridgeToolHostAffinity
    {
        public string Host { get; set; } = string.Empty;

        public bool IsHostSpecific => !string.IsNullOrWhiteSpace(Host);
    }
}
