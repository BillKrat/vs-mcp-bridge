using Microsoft.Extensions.Logging;
using System;

namespace VsMcpBridge.Shared.Loggers;

public sealed class BridgeLogEntry
{
    public DateTime TimestampUtc { get; set; }

    public LogLevel Level { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public EventId EventId { get; set; }

    public string Message { get; set; } = string.Empty;

    public Exception? Exception { get; set; }
}
