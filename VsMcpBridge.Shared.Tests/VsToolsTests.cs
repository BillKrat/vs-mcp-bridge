using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VsMcpBridge.McpServer.Tools;
using VsMcpBridge.Shared.Interfaces;
using VsMcpBridge.Shared.Models;
using Xunit;

namespace VsMcpBridge.Shared.Tests;

public sealed class VsToolsTests
{
    [Fact]
    public async Task ProposeTextEditAsync_preserves_backward_compatible_single_file_request_flow()
    {
        var pipeClient = new RecordingPipeClient();
        var tools = new VsTools(pipeClient);

        var response = await tools.ProposeTextEditAsync("sample.cs", "before", "after", CancellationToken.None);

        Assert.Equal(1, pipeClient.ProposeTextEditCalls);
        Assert.Equal(0, pipeClient.ProposeTextEditsCalls);
        Assert.Contains("Proposed diff for sample.cs", response, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProposeTextEditsAsync_sends_multi_file_request_through_pipe_client()
    {
        var pipeClient = new RecordingPipeClient();
        var tools = new VsTools(pipeClient);
        var fileEdits = new[]
        {
            new ProposalFileEditRequest { FilePath = "first.cs", OriginalText = "before-1", ProposedText = "after-1" },
            new ProposalFileEditRequest { FilePath = "second.cs", OriginalText = "before-2", ProposedText = "after-2" }
        };

        var response = await tools.ProposeTextEditsAsync(fileEdits, CancellationToken.None);

        Assert.Equal(0, pipeClient.ProposeTextEditCalls);
        Assert.Equal(1, pipeClient.ProposeTextEditsCalls);
        Assert.Equal(2, pipeClient.LastFileEdits.Count);
        Assert.Contains("Proposed diff for 2 files", response, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProposeTextEditsAsync_returns_clear_error_for_invalid_payload()
    {
        var pipeClient = new RecordingPipeClient();
        var tools = new VsTools(pipeClient);

        var response = await tools.ProposeTextEditsAsync(
            new[] { new ProposalFileEditRequest { FilePath = string.Empty, OriginalText = "before", ProposedText = "after" } },
            CancellationToken.None);

        Assert.Equal("Error: each file edit must include a non-empty filePath.", response);
        Assert.Equal(0, pipeClient.ProposeTextEditCalls);
        Assert.Equal(0, pipeClient.ProposeTextEditsCalls);
    }

    private sealed class RecordingPipeClient : IPipeClient
    {
        public int ProposeTextEditCalls { get; private set; }
        public int ProposeTextEditsCalls { get; private set; }
        public IReadOnlyList<ProposalFileEditRequest> LastFileEdits { get; private set; } = Array.Empty<ProposalFileEditRequest>();

        public Task<GetActiveDocumentResponse> GetActiveDocumentAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task<GetErrorListResponse> GetErrorListAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task<GetSelectedTextResponse> GetSelectedTextAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task<ListSolutionProjectsResponse> ListSolutionProjectsAsync(CancellationToken ct = default) => throw new NotSupportedException();

        public Task<ProposeTextEditResponse> ProposeTextEditAsync(string filePath, string originalText, string proposedText, CancellationToken ct = default)
        {
            ProposeTextEditCalls++;
            return Task.FromResult(new ProposeTextEditResponse
            {
                Success = true,
                FilePath = filePath,
                Diff = $"--- a/{filePath}\n+++ b/{filePath}\n-{originalText}\n+{proposedText}\n"
            });
        }

        public Task<ProposeTextEditResponse> ProposeTextEditsAsync(IReadOnlyList<ProposalFileEditRequest> fileEdits, CancellationToken ct = default)
        {
            ProposeTextEditsCalls++;
            LastFileEdits = fileEdits;
            return Task.FromResult(new ProposeTextEditResponse
            {
                Success = true,
                FilePath = fileEdits[0].FilePath,
                Diff = string.Join("\n", fileEdits.Select(fileEdit => $"--- a/{fileEdit.FilePath}\n+++ b/{fileEdit.FilePath}\n-{fileEdit.OriginalText}\n+{fileEdit.ProposedText}\n"))
            });
        }
    }
}
