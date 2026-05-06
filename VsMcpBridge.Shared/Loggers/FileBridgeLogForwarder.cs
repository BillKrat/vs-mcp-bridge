using System;
using System.IO;
using System.Text;
using VsMcpBridge.Shared.Constants;
using VsMcpBridge.Shared.Interfaces;

namespace VsMcpBridge.Shared.Loggers;

public sealed class FileBridgeLogForwarder : IBridgeLogForwarder
{
    private readonly object _sync = new();
    private readonly string _logFilePath;

    public FileBridgeLogForwarder()
        : this(GetDefaultLogFilePath())
    {
    }

    public FileBridgeLogForwarder(string logFilePath)
    {
        _logFilePath = logFilePath ?? throw new ArgumentNullException(nameof(logFilePath));
    }

    public void Forward(BridgeLogEntry entry)
    {
        if (entry == null)
            throw new ArgumentNullException(nameof(entry));

        var directory = Path.GetDirectoryName(_logFilePath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var line = BridgeLogFormatter.FormatLine(entry.TimestampUtc, entry.Level, entry.CategoryName, entry.Message);
        var exceptionText = entry.Exception == null ? string.Empty : Environment.NewLine + entry.Exception;

        lock (_sync)
        {
            File.AppendAllText(_logFilePath, line + exceptionText + Environment.NewLine, Encoding.UTF8);
        }
    }

    private static string GetDefaultLogFilePath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, BridgeRuntimeConstants.PipeName, "Logs", "Bridge", "bridge-log-forwarder.log");
    }
}
