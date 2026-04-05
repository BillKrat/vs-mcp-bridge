using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Shell;
using System;

namespace VsMcpBridge.Vsix.Logging;

public sealed class ActivityLogBridgeLogger : ILogger
{
    private const string Source = nameof(VsMcpBridgePackage);

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        switch (logLevel)
        {
            case LogLevel.Trace:
            case LogLevel.Debug:
            case LogLevel.Information:
                ActivityLog.TryLogInformation(Source, message);
                break;
            case LogLevel.Warning:
                ActivityLog.TryLogWarning(Source, message);
                break;
            case LogLevel.Error:
            case LogLevel.Critical:
                var details = exception == null ? message : $"{message}{Environment.NewLine}{exception}";
                ActivityLog.TryLogError(Source, details);
                break;
        }
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
}
