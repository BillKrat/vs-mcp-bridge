using System;

namespace VsMcpBridge.Vsix.Logging;

public interface IBridgeLogger
{
    void LogVerbose(string message);
    void LogInformation(string message);
    void LogWarning(string message);
    void LogError(string message, Exception? exception = null);
}
