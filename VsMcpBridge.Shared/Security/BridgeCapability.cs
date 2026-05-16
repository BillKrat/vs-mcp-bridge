namespace VsMcpBridge.Shared.Security
{
    public sealed class BridgeCapability
    {
        public BridgeCapability(string name)
        {
            Name = name ?? string.Empty;
        }

        public string Name { get; }
    }
}
