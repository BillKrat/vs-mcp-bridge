using Microsoft.Extensions.Logging;
using System;

namespace VsMcpBridge.Shared.Loggers;

public sealed class ConsoleBridgeLogger : ILogger
{
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        var level = logLevel switch
        {
            LogLevel.Trace => "TRACE",
            LogLevel.Debug => "DEBUG",
            LogLevel.Information => "INFO",
            LogLevel.Warning => "WARN",
            LogLevel.Error => "ERROR",
            LogLevel.Critical => "CRIT",
            _ => logLevel.ToString().ToUpperInvariant()
        };
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [{level}] {message}");
        if (exception is not null)
            Console.Error.WriteLine(exception);
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
}
