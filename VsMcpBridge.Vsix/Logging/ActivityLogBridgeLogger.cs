using Microsoft.VisualStudio.Shell;
using System;

namespace VsMcpBridge.Vsix.Logging;

public sealed class ActivityLogBridgeLogger : IBridgeLogger
{
    private const string Source = nameof(VsMcpBridgePackage);

    public void LogVerbose(string message)
    {
        ActivityLog.TryLogInformation(Source, $"[Verbose] {message}");
    }

    public void LogInformation(string message)
    {
        ActivityLog.TryLogInformation(Source, message);
    }

    public void LogWarning(string message)
    {
        ActivityLog.TryLogWarning(Source, message);
    }

    public void LogError(string message, Exception? exception = null)
    {
        var details = exception == null ? message : $"{message}{Environment.NewLine}{exception}";
        ActivityLog.TryLogError(Source, details);
    }
}
