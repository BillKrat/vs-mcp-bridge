using System;
using System.Collections.Generic;

namespace VsMcpBridge.Shared.Security
{
    public sealed class BridgeAuditEnvelope
    {
        public DateTimeOffset TimestampUtc { get; set; } = DateTimeOffset.UtcNow;

        public string EventName { get; set; } = string.Empty;

        public string ToolId { get; set; } = string.Empty;

        public string RequestId { get; set; } = string.Empty;

        public string OperationId { get; set; } = string.Empty;

        public bool Success { get; set; }

        public bool Allowed { get; set; }

        public string ErrorCode { get; set; } = string.Empty;

        public long ElapsedMilliseconds { get; set; }

        public IReadOnlyDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }
}
