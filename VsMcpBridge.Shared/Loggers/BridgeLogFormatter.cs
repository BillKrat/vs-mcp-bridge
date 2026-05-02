using Microsoft.Extensions.Logging;
using System;

namespace VsMcpBridge.Shared.Loggers;

public static class BridgeLogFormatter
{
    public static string FormatLine(DateTime timestampUtc, LogLevel level, string categoryName, string message)
    {
        var safeCategory = string.IsNullOrWhiteSpace(categoryName) ? "Bridge" : categoryName;
        var safeMessage = string.IsNullOrWhiteSpace(message) ? string.Empty : message;
        return $"[{timestampUtc:O}] [{level}] [{safeCategory}] {safeMessage}";
    }
}
