using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using VsMcpBridge.Shared.Models;
using VsMcpBridge.Vsix.Pipe;
using VsMcpBridge.Vsix.Tests.Support;
using Xunit;

namespace VsMcpBridge.Vsix.Tests;

public sealed class PipeServerTests
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    [Fact]
    public async Task ProcessRequestAsync_logs_first_request_and_dispatches_active_document()
    {
        var logger = new RecordingBridgeLogger();
        var service = new StubVsService();
        var server = new PipeServer(service, logger);
        var requestJson = JsonSerializer.Serialize(new PipeMessage { Command = PipeCommands.GetActiveDocument, Payload = string.Empty }, JsonOptions);

        var responseJson = await server.ProcessRequestAsync(requestJson);
        var response = JsonSerializer.Deserialize<GetActiveDocumentResponse>(responseJson!, JsonOptions);

        Assert.NotNull(response);
        Assert.True(response!.Success);
        Assert.Equal("active.cs", response.FilePath);
        Assert.Equal(1, service.GetActiveDocumentCalls);
        Assert.Contains("Handling first bridge request.", logger.VerboseMessages);
        Assert.Contains(logger.InformationMessages, message => message.Contains("Dispatching pipe command 'vs_get_active_document'"));
    }

    [Fact]
    public async Task ProcessRequestAsync_only_logs_first_request_once()
    {
        var logger = new RecordingBridgeLogger();
        var service = new StubVsService();
        var server = new PipeServer(service, logger);
        var requestJson = JsonSerializer.Serialize(new PipeMessage { Command = PipeCommands.GetSelectedText, Payload = string.Empty }, JsonOptions);

        await server.ProcessRequestAsync(requestJson);
        await server.ProcessRequestAsync(requestJson);

        Assert.Equal(1, logger.VerboseMessages.Count(message => message == "Handling first bridge request."));
        Assert.Equal(2, service.GetSelectedTextCalls);
    }

    [Fact]
    public async Task ProcessRequestAsync_returns_null_for_empty_request_and_logs_warning()
    {
        var logger = new RecordingBridgeLogger();
        var server = new PipeServer(new StubVsService(), logger);

        var responseJson = await server.ProcessRequestAsync(string.Empty);

        Assert.Null(responseJson);
        Assert.Contains("Received an empty pipe request.", logger.WarningMessages);
    }

    [Fact]
    public async Task ProcessRequestAsync_returns_error_response_for_unknown_command()
    {
        var logger = new RecordingBridgeLogger();
        var server = new PipeServer(new StubVsService(), logger);
        var requestJson = JsonSerializer.Serialize(new PipeMessage { Command = "unknown_command", Payload = string.Empty }, JsonOptions);

        var responseJson = await server.ProcessRequestAsync(requestJson);
        using var document = JsonDocument.Parse(responseJson!);

        Assert.False(document.RootElement.GetProperty("success").GetBoolean());
        Assert.Contains("Unknown command", document.RootElement.GetProperty("errorMessage").GetString());
    }

    [Fact]
    public async Task ProcessRequestAsync_dispatches_propose_text_edit_requests()
    {
        var logger = new RecordingBridgeLogger();
        var service = new StubVsService();
        var server = new PipeServer(service, logger);
        var payload = JsonSerializer.Serialize(new ProposeTextEditRequest
        {
            FilePath = "sample.cs",
            OriginalText = "before",
            ProposedText = "after"
        }, JsonOptions);
        var requestJson = JsonSerializer.Serialize(new PipeMessage { Command = PipeCommands.ProposeTextEdit, Payload = payload }, JsonOptions);

        var responseJson = await server.ProcessRequestAsync(requestJson);
        var response = JsonSerializer.Deserialize<ProposeTextEditResponse>(responseJson!, JsonOptions);

        Assert.NotNull(response);
        Assert.True(response!.Success);
        Assert.Equal(1, service.ProposeTextEditCalls);
        Assert.Contains("sample.cs", response.Diff);
    }
}
