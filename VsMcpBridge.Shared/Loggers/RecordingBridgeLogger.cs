using System;
using System.Collections.Generic;
using VsMcpBridge.Shared.Interfaces;

namespace VsMcpBridge.Shared.Loggers;

public sealed class RecordingBridgeLogger : IBridgeLogger
{
    public List<string> VerboseMessages { get; } = new();
    public List<string> InformationMessages { get; } = new();
    public List<string> WarningMessages { get; } = new();
    public List<(string Message, Exception? Exception)> Errors { get; } = new();

    public void LogVerbose(string message) => VerboseMessages.Add(message);

    public void LogInformation(string message) => InformationMessages.Add(message);

    public void LogWarning(string message) => WarningMessages.Add(message);

    public void LogError(string message, Exception? exception = null) => Errors.Add((message, exception));
}
