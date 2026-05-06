using VsMcpBridge.Shared.Interfaces;

namespace VsMcpBridge.Shared.Loggers;

public sealed class NullBridgeLogForwarder : IBridgeLogForwarder
{
    public void Forward(BridgeLogEntry entry)
    {
    }
}
