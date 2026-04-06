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
