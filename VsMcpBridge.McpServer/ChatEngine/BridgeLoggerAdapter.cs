using Microsoft.Extensions.Logging;

namespace VsMcpBridge.McpServer.ChatEngine;

internal sealed class BridgeLoggerAdapter<T>(ILogger logger) : ILogger<T>
{
    private readonly ILogger logger = logger;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        => this.logger.BeginScope(state);

    public bool IsEnabled(LogLevel logLevel)
        => this.logger.IsEnabled(logLevel);

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        this.logger.Log(logLevel, eventId, state, exception, formatter);
    }
}
