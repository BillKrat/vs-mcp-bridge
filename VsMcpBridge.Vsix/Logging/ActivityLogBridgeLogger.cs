using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Shell;
using System;
using VsMcpBridge.Shared.Interfaces;
using VsMcpBridge.Shared.Loggers;

namespace VsMcpBridge.Vsix.Logging;

public class ActivityLogBridgeLogger : LoggerBase
{
    protected override string Source { get; set; } = nameof(VsMcpBridgePackage);

    public ActivityLogBridgeLogger(ILogLevelSettings settings)
    {
        Settings = settings;
        AdditionalLogger = new DebugBridgeLogger(Settings);
    }

    public ActivityLogBridgeLogger()
    {
        AdditionalLogger = new DebugBridgeLogger(Settings);
    }

    protected override void LogMessage(LogLevel level, string source, string message, Exception? exception = null)
    {
        switch (level)
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
}

public sealed class ActivityLogBridgeLoggerProvider : ILoggerProvider
{
    private readonly ILogLevelSettings _settings;

    public ActivityLogBridgeLoggerProvider(ILogLevelSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public ILogger CreateLogger(string categoryName)
        => new ActivityLogProviderLogger(categoryName, _settings);

    public void Dispose()
    {
    }

    private sealed class ActivityLogProviderLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly ILogLevelSettings _settings;

        public ActivityLogProviderLogger(string categoryName, ILogLevelSettings settings)
        {
            _categoryName = categoryName ?? string.Empty;
            _settings = settings;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel)
            => logLevel != LogLevel.None && logLevel >= _settings.MinimumLevel;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel) || formatter == null)
                return;

            var message = formatter(state, exception);
            var line = BridgeLogFormatter.FormatLine(DateTime.UtcNow, logLevel, _categoryName, message);

            switch (logLevel)
            {
                case LogLevel.Trace:
                case LogLevel.Debug:
                case LogLevel.Information:
                    ActivityLog.TryLogInformation(nameof(VsMcpBridgePackage), line);
                    break;
                case LogLevel.Warning:
                    ActivityLog.TryLogWarning(nameof(VsMcpBridgePackage), line);
                    break;
                case LogLevel.Error:
                case LogLevel.Critical:
                    ActivityLog.TryLogError(nameof(VsMcpBridgePackage), exception == null ? line : $"{line}{Environment.NewLine}{exception}");
                    break;
            }

            System.Diagnostics.Debug.WriteLine(line);
            if (exception != null)
                System.Diagnostics.Debug.WriteLine(exception.ToString());
        }
    }
}
