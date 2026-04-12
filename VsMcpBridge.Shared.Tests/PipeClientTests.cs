using System;
using System.Threading;
using System.Threading.Tasks;
using VsMcpBridge.McpServer.Pipe;
using VsMcpBridge.Shared.Loggers;
using Xunit;

namespace VsMcpBridge.Shared.Tests;

public sealed class PipeClientTests
{
    [Fact]
    public async Task GetActiveDocumentAsync_when_pipe_is_unavailable_logs_trace_and_throws()
    {
        var logger = new RecordingBridgeLogger();
        var pipeName = $"VsMcpBridge-Test-{Guid.NewGuid():N}";
        var client = new PipeClient(logger, pipeName);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(6));

        await Assert.ThrowsAnyAsync<Exception>(() => client.GetActiveDocumentAsync(cts.Token));

        Assert.Contains(logger.VerboseMessages, message => message.Contains($"Attempting pipe connection to '{pipeName}'", StringComparison.Ordinal));
        Assert.Contains(logger.VerboseMessages, message => message.Contains("Pipe request failed", StringComparison.Ordinal));
        Assert.Empty(logger.InformationMessages);
        Assert.Empty(logger.WarningMessages);
        Assert.Empty(logger.Errors);
    }
}
