using Microsoft.Extensions.Logging;
using System;
using VsMcpBridge.Shared.Interfaces;

namespace VsMcpBridge.Shared.Loggers;

public sealed class UiBridgeLoggerProvider : ILoggerProvider
{
    private readonly IBridgeLogSink _logSink;
    private readonly ILogLevelSettings _settings;

    public UiBridgeLoggerProvider(IBridgeLogSink logSink, ILogLevelSettings settings)
    {
        _logSink = logSink ?? throw new ArgumentNullException(nameof(logSink));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public ILogger CreateLogger(string categoryName)
        => new UiBridgeLogger(categoryName, _logSink, _settings);

    public void Dispose()
    {
    }

    private sealed class UiBridgeLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly IBridgeLogSink _logSink;
        private readonly ILogLevelSettings _settings;

        public UiBridgeLogger(string categoryName, IBridgeLogSink logSink, ILogLevelSettings settings)
        {
            _categoryName = categoryName ?? string.Empty;
            _logSink = logSink;
            _settings = settings;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel)
            => logLevel != LogLevel.None && logLevel >= _settings.MinimumLevel;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel) || formatter == null)
                return;

            var message = formatter(state, exception);
            _logSink.Publish(new BridgeLogEntry
            {
                TimestampUtc = DateTime.UtcNow,
                Level = logLevel,
                CategoryName = _categoryName,
                EventId = eventId,
                Message = message,
                Exception = exception
            });
        }
    }
}
