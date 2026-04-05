using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace VsMcpBridge.Shared.Loggers;

public sealed class RecordingBridgeLogger : ILogger
{
    public List<string> VerboseMessages { get; } = new();
    public List<string> InformationMessages { get; } = new();
    public List<string> WarningMessages { get; } = new();
    public List<(string Message, Exception? Exception)> Errors { get; } = new();

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        switch (logLevel)
        {
            case LogLevel.Trace:
                VerboseMessages.Add(message);
                break;
            case LogLevel.Information:
                InformationMessages.Add(message);
                break;
            case LogLevel.Warning:
                WarningMessages.Add(message);
                break;
            case LogLevel.Error:
            case LogLevel.Critical:
                Errors.Add((message, exception));
                break;
        }
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
}
