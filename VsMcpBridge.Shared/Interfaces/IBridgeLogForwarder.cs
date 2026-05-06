using VsMcpBridge.Shared.Loggers;

namespace VsMcpBridge.Shared.Interfaces;

public interface IBridgeLogForwarder
{
    void Forward(BridgeLogEntry entry);
}
