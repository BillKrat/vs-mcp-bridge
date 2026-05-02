using Microsoft.Extensions.Logging;
using System;
using VsMcpBridge.Shared.Interfaces;

namespace VsMcpBridge.Shared.Loggers;

public sealed class DebugBridgeLoggerProvider : ILoggerProvider
{
    private readonly ILogLevelSettings _settings;

    public DebugBridgeLoggerProvider(ILogLevelSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public ILogger CreateLogger(string categoryName)
        => new DebugProviderLogger(categoryName, _settings);

    public void Dispose()
    {
    }

    private sealed class DebugProviderLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly ILogLevelSettings _settings;

        public DebugProviderLogger(string categoryName, ILogLevelSettings settings)
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
            System.Diagnostics.Debug.WriteLine(line);
            if (exception != null)
                System.Diagnostics.Debug.WriteLine(exception.ToString());
        }
    }
}
