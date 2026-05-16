using System.Collections.Generic;

namespace VsMcpBridge.Shared.Tools
{
    public sealed class BridgeToolDiscoveryOptions
    {
        public bool EnableMefDirectoryDiscovery { get; set; }

        public IList<string> MefDirectories { get; } = new List<string>();

        public string MefSearchPattern { get; set; } = "*.dll";
    }
}
