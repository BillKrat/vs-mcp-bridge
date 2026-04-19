using Microsoft.Extensions.Logging;
using System;
using VsMcpBridge.Shared.Interfaces;

// TODO: bridge round-trip validation
namespace VsMcpBridge.Shared.Loggers
{
    public class LoggerBase : ILogger
    {
        protected virtual ILogLevelSettings Settings { get; set; }
        protected virtual string Source { get; set; }

        protected LoggerBase? AdditionalLogger { get; set; }

        protected LoggerBase(ILogLevelSettings? settings = null)
        {
            Settings = settings ?? new LogLevelSettings();
            Source = GetType().Name;
        }

        public bool IsEnabled(LogLevel logLevel) =>
            logLevel != LogLevel.None && logLevel >= Settings.MinimumLevel;

        public virtual IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public virtual void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            var message = formatter(state, exception);
            switch (logLevel)
            {
                case LogLevel.Trace:
                    LogTrace(Source, message);
                    break;
                case LogLevel.Debug:
                    LogDebug(Source, message);
                    break;
                case LogLevel.Information:
                    LogInformation(Source, message);
                    break;
                case LogLevel.Warning:
                    LogWarning(Source, message);
                    break;
                case LogLevel.Error:
                    LogError(Source, message, exception);
                    break;
                case LogLevel.Critical:
                    var details = exception == null ? message : $"{message}{Environment.NewLine}{exception}";
                    LogCritical(Source, details);
                    break;
            }
        }

        protected virtual void LogCritical(string source, string message)
        {
            LogMessage(LogLevel.Critical, Source, message);
            AdditionalLogger?.LogCritical(source, message);
        }

        protected virtual void LogError(string source, string message, Exception? exception)
        {
            LogMessage(LogLevel.Error, Source, message, exception);
            AdditionalLogger?.LogError(source, message, exception);
        }

        protected virtual void LogWarning(string source, string message)
        {
            LogMessage(LogLevel.Warning, Source, message);
            AdditionalLogger?.LogWarning(source, message);
        }

        protected virtual void LogInformation(string source, string message)
        {
            LogMessage(LogLevel.Information, Source, message);
            AdditionalLogger?.LogInformation(source, message);
        }

        protected virtual void LogDebug(string source, string message)
        {
            LogMessage(LogLevel.Debug, Source, message);
            AdditionalLogger?.LogDebug(source, message);
        }
        protected virtual void LogTrace(string source, string message)
        {
            LogMessage(LogLevel.Trace, Source, message);
            AdditionalLogger?.LogTrace(source, message);
        }


        protected virtual void LogMessage(LogLevel level, string source, string message, Exception? exception = null)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [{level}] [{source}] {message}");
            if (exception is not null)
                Console.Error.WriteLine(exception);
        }
    }
}
