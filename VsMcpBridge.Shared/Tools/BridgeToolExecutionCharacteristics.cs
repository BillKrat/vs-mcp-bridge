namespace VsMcpBridge.Shared.Tools
{
    public sealed class BridgeToolExecutionCharacteristics
    {
        public string Source { get; set; } = string.Empty;

        public BridgeToolDiscoveryKind DiscoveryKind { get; set; } = BridgeToolDiscoveryKind.Unspecified;

        public bool ExecutesThroughBridgeToolExecutor { get; set; } = true;
    }
}
