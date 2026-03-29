using System;
using System.Collections.Generic;
using VsMcpBridge.Vsix.Logging;

namespace VsMcpBridge.Vsix.Tests.Support;

internal sealed class RecordingBridgeLogger : IBridgeLogger
{
    public List<string> VerboseMessages { get; } = new();
    public List<string> InformationMessages { get; } = new();
    public List<string> WarningMessages { get; } = new();
    public List<string> ErrorMessages { get; } = new();

    public void LogVerbose(string message) => VerboseMessages.Add(message);
    public void LogInformation(string message) => InformationMessages.Add(message);
    public void LogWarning(string message) => WarningMessages.Add(message);
    public void LogError(string message, Exception? exception = null)
        => ErrorMessages.Add(exception == null ? message : $"{message}: {exception.Message}");
}
