using System;
using System.Globalization;
using System.IO;
using System.Text;
using VsMcpBridge.Vsix.Logging;

namespace VsMcpBridge.Vsix.Diagnostics;

public sealed class FileUnhandledExceptionSink : IUnhandledExceptionSink
{
    private readonly IBridgeLogger _logger;
    private readonly Func<DateTimeOffset> _clock;
    private readonly string _logDirectory;

    public FileUnhandledExceptionSink(IBridgeLogger logger)
        : this(logger, () => DateTimeOffset.UtcNow, GetDefaultLogDirectory())
    {
    }

    internal FileUnhandledExceptionSink(IBridgeLogger logger, Func<DateTimeOffset> clock, string logDirectory)
    {
        _logger = logger;
        _clock = clock;
        _logDirectory = logDirectory;
    }

    public void Save(string source, Exception exception)
    {
        var timestamp = _clock();
        Directory.CreateDirectory(_logDirectory);

        var fileName = string.Format(
            CultureInfo.InvariantCulture,
            "unhandled-{0:yyyyMMdd-HHmmssfff}.log",
            timestamp);

        var path = Path.Combine(_logDirectory, fileName);
        File.AppendAllText(path, BuildContents(source, exception, timestamp), Encoding.UTF8);
        _logger.LogInformation($"Unhandled exception details written to '{path}'.");
    }

    private static string GetDefaultLogDirectory()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, "VsMcpBridge", "Logs", "UnhandledExceptions");
    }

    private static string BuildContents(string source, Exception exception, DateTimeOffset timestamp)
    {
        var builder = new StringBuilder();
        builder.AppendLine("VS MCP Bridge Unhandled Exception");
        builder.AppendLine($"TimestampUtc: {timestamp:O}");
        builder.AppendLine($"Source: {source}");
        builder.AppendLine($"ExceptionType: {exception.GetType().FullName}");
        builder.AppendLine($"Message: {exception.Message}");
        builder.AppendLine("StackTrace:");
        builder.AppendLine(exception.StackTrace ?? "<none>");

        var inner = exception.InnerException;
        var depth = 0;
        while (inner != null)
        {
            depth++;
            builder.AppendLine($"InnerException[{depth}].Type: {inner.GetType().FullName}");
            builder.AppendLine($"InnerException[{depth}].Message: {inner.Message}");
            builder.AppendLine($"InnerException[{depth}].StackTrace:");
            builder.AppendLine(inner.StackTrace ?? "<none>");
            inner = inner.InnerException;
        }

        builder.AppendLine();
        return builder.ToString();
    }
}
