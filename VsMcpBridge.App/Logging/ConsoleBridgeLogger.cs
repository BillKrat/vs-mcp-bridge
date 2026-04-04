using System;
using VsMcpBridge.Shared.Interfaces;

namespace VsMcpBridge.App.Logging;

internal sealed class ConsoleBridgeLogger : IBridgeLogger
{
    public void LogVerbose(string message) => Log("VERBOSE", message);
    public void LogInformation(string message) => Log("INFO", message);
    public void LogWarning(string message) => Log("WARN", message);

    public void LogError(string message, Exception? exception = null)
    {
        Log("ERROR", message);
        if (exception is not null)
            Console.Error.WriteLine(exception);
    }

    private static void Log(string level, string message) =>
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [{level}] {message}");
}
