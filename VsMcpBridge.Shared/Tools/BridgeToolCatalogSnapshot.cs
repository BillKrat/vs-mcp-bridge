using System;
using System.Collections.Generic;

namespace VsMcpBridge.Shared.Tools
{
    public sealed class BridgeToolCatalogSnapshot
    {
        public DateTimeOffset CapturedAtUtc { get; set; } = DateTimeOffset.UtcNow;

        public IReadOnlyList<BridgeToolInventoryItem> Tools { get; set; } = Array.Empty<BridgeToolInventoryItem>();
    }
}
