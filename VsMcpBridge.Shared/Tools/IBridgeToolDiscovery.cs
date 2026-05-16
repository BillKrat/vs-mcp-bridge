using System.Collections.Generic;

namespace VsMcpBridge.Shared.Tools
{
    public interface IBridgeToolDiscovery
    {
        IReadOnlyList<IBridgeTool> DiscoverTools();
    }
}
