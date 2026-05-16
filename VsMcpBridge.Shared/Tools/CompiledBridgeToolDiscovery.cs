using System;
using System.Collections.Generic;
using System.Linq;

namespace VsMcpBridge.Shared.Tools
{
    public sealed class CompiledBridgeToolDiscovery : IBridgeToolDiscovery
    {
        private readonly IEnumerable<IBridgeTool> _tools;

        public CompiledBridgeToolDiscovery(IEnumerable<IBridgeTool> tools)
        {
            _tools = tools ?? throw new ArgumentNullException(nameof(tools));
        }

        public IReadOnlyList<IBridgeTool> DiscoverTools()
            => _tools.ToArray();
    }
}
