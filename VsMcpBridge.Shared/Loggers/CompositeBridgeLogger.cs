using Microsoft.Extensions.Logging;
using System;

namespace VsMcpBridge.Shared.Loggers;

public sealed class CompositeBridgeLogger : ILogger
{
    private readonly ILogger _primary;
    private readonly ILogger _secondary;

    public CompositeBridgeLogger(ILogger primary, ILogger secondary)
    {
        _primary = primary ?? throw new ArgumentNullException(nameof(primary));
        _secondary = secondary ?? throw new ArgumentNullException(nameof(secondary));
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        => _primary.BeginScope(state);

    public bool IsEnabled(LogLevel logLevel)
        => _primary.IsEnabled(logLevel) || _secondary.IsEnabled(logLevel);

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        _primary.Log(logLevel, eventId, state, exception, formatter);
        _secondary.Log(logLevel, eventId, state, exception, formatter);
    }
}
