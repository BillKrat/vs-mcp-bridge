using System;
using System.IO;
using VsMcpBridge.Vsix.Diagnostics;
using VsMcpBridge.Vsix.Tests.Support;
using Xunit;

namespace VsMcpBridge.Vsix.Tests;

public sealed class FileUnhandledExceptionSinkTests
{
    [Fact]
    public void Save_writes_verbose_exception_report_to_disk()
    {
        var logger = new RecordingBridgeLogger();
        var directory = Path.Combine(Path.GetTempPath(), "VsMcpBridge.Tests", Guid.NewGuid().ToString("N"));
        var sink = new FileUnhandledExceptionSink(
            logger,
            () => new DateTimeOffset(2026, 3, 29, 12, 34, 56, TimeSpan.Zero),
            directory);

        try
        {
            var exception = new InvalidOperationException("outer failure", new ArgumentException("inner failure"));

            sink.Save("UnitTest", exception);

            var files = Directory.GetFiles(directory, "*.log");
            Assert.Single(files);

            var contents = File.ReadAllText(files[0]);
            Assert.Contains("VS MCP Bridge Unhandled Exception", contents);
            Assert.Contains("Source: UnitTest", contents);
            Assert.Contains("ExceptionType: System.InvalidOperationException", contents);
            Assert.Contains("Message: outer failure", contents);
            Assert.Contains("InnerException[1].Type: System.ArgumentException", contents);
            Assert.Contains("InnerException[1].Message: inner failure", contents);
            Assert.Contains(logger.InformationMessages, message => message.Contains(files[0]));
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }
}
