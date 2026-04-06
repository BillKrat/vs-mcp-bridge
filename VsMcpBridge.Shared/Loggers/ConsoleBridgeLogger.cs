using Microsoft.Extensions.Logging;
using System;
using VsMcpBridge.Shared.Interfaces;

namespace VsMcpBridge.Shared.Loggers;

public sealed class ConsoleBridgeLogger : LoggerBase
{
    public ConsoleBridgeLogger(ILogLevelSettings settings) : base(settings) { }

    public ConsoleBridgeLogger() : base() { }

    protected override void LogMessage(LogLevel level, string source, string message, Exception? exception = null)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [{level}] {message}");
        if (exception is not null)
            Console.Error.WriteLine(exception);
    }
}
