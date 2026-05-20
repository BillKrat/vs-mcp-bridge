namespace VsMcpBridge.Shared.Tools
{
    public sealed class BridgeToolIdentity
    {
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Version { get; set; } = BridgeToolManifest.DefaultVersion;
    }
}
