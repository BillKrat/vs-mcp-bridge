using System.Collections.Generic;

namespace VsMcpBridge.Shared.Tools
{
    public interface IBridgeToolCatalog
    {
        IReadOnlyList<BridgeToolDescriptor> GetTools();
        bool TryGetTool(string toolId, out IBridgeTool tool);
    }
}
