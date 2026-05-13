using System.Collections.Generic;

namespace VsMcpBridge.Shared.Tools
{
    public sealed class BridgeToolRequest
    {
        public string ToolId { get; set; } = string.Empty;
        public string RequestId { get; set; } = string.Empty;
        public string OperationId { get; set; } = string.Empty;
        public IReadOnlyDictionary<string, object?> Arguments { get; set; } = new Dictionary<string, object?>();
    }
}
