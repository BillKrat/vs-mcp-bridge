using Microsoft.Extensions.Logging;
using System;
using VsMcpBridge.Shared.Interfaces;

namespace VsMcpBridge.Shared.Loggers;

public sealed class DebugBridgeLogger : LoggerBase
{
    public DebugBridgeLogger(ILogLevelSettings? settings = null) : base(settings)
    {
    }

    protected override void LogMessage(LogLevel level, string source, string message, Exception? exception = null)
    {
        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now:HH:mm:ss}] [{level}] {message}");
        if (exception is not null)
            System.Diagnostics.Debug.WriteLine(exception.ToString());
    }
}
