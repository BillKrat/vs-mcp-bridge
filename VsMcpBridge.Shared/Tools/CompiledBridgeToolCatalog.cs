using System;
using System.Collections.Generic;
using System.Linq;

namespace VsMcpBridge.Shared.Tools
{
    public sealed class CompiledBridgeToolCatalog : IBridgeToolCatalog
    {
        private readonly Dictionary<string, IBridgeTool> _tools;

        public CompiledBridgeToolCatalog(IEnumerable<IBridgeToolDiscovery> discoveries)
            : this(DiscoverTools(discoveries))
        {
        }

        public CompiledBridgeToolCatalog(IEnumerable<IBridgeTool> tools)
        {
            if (tools == null)
                throw new ArgumentNullException(nameof(tools));

            _tools = new Dictionary<string, IBridgeTool>(StringComparer.Ordinal);
            foreach (var tool in tools)
            {
                var toolId = tool.Descriptor.Id;
                if (_tools.TryGetValue(toolId, out var existingTool))
                {
                    throw new InvalidOperationException(
                        $"Duplicate bridge tool id '{toolId}' discovered. ExistingTool={existingTool.GetType().FullName}; DuplicateTool={tool.GetType().FullName}.");
                }

                _tools.Add(toolId, tool);
            }
        }

        public IReadOnlyList<BridgeToolDescriptor> GetTools()
            => _tools.Values.Select(tool => tool.Descriptor).ToArray();

        public bool TryGetTool(string toolId, out IBridgeTool tool)
            => _tools.TryGetValue(toolId, out tool!);

        private static IEnumerable<IBridgeTool> DiscoverTools(IEnumerable<IBridgeToolDiscovery> discoveries)
        {
            if (discoveries == null)
                throw new ArgumentNullException(nameof(discoveries));

            return discoveries.SelectMany(discovery => discovery.DiscoverTools()).ToArray();
        }
    }
}
