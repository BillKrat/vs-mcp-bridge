using System;
using System.Collections.Generic;
using System.Linq;

namespace VsMcpBridge.Shared.Tools
{
    public sealed class CompiledBridgeToolCatalog : IBridgeToolCatalog
    {
        private readonly Dictionary<string, IBridgeTool> _tools;

        public CompiledBridgeToolCatalog(IEnumerable<IBridgeTool> tools)
        {
            if (tools == null)
                throw new ArgumentNullException(nameof(tools));

            _tools = tools.ToDictionary(
                tool => tool.Descriptor.Id,
                tool => tool,
                StringComparer.Ordinal);
        }

        public IReadOnlyList<BridgeToolDescriptor> GetTools()
            => _tools.Values.Select(tool => tool.Descriptor).ToArray();

        public bool TryGetTool(string toolId, out IBridgeTool tool)
            => _tools.TryGetValue(toolId, out tool!);
    }
}
