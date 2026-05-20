using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Adventures.ChatEngine.Abstractions;
using Adventures.ChatEngine.Events;
using Adventures.ChatEngine.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using VsMcpBridge.McpServer.Tools;
using VsMcpBridge.Shared.Composition;
using VsMcpBridge.Shared.Interfaces;
using VsMcpBridge.Shared.Loggers;
using VsMcpBridge.Shared.Models;
using VsMcpBridge.Shared.Security;
using VsMcpBridge.Shared.Tools;
using Xunit;

namespace VsMcpBridge.Shared.Tests;

public sealed class VsToolsTests
{
    [Fact]
    public void GetToolInventory_returns_deterministic_manifest_metadata_without_executing_tools()
    {
        var alpha = new InventoryMcpProbeBridgeTool(
            "fake.alpha",
            category: "Diagnostics",
            discoveryKind: BridgeToolDiscoveryKind.Compiled,
            source: "Compiled",
            host: "SharedTests",
            requiredCapabilities: new[] { new BridgeCapability("workspace.read"), new BridgeCapability("diagnostics.read") },
            approvalRequirement: ToolExecutionApprovalRequirement.Required);
        var zeta = new InventoryMcpProbeBridgeTool(
            "fake.zeta",
            category: "Search",
            discoveryKind: BridgeToolDiscoveryKind.Mef,
            source: "MEF",
            host: "ExternalTests",
            requiredCapabilities: Array.Empty<BridgeCapability>(),
            approvalRequirement: ToolExecutionApprovalRequirement.NotRequired);
        var inventory = new BridgeToolInventoryService(new CompiledBridgeToolCatalog(new IBridgeTool[] { zeta, alpha }));
        var pipeClient = new RecordingPipeClient();
        var chatEngine = new StubChatEngine();
        var logger = new RecordingBridgeLogger();
        var tools = new VsTools(pipeClient, chatEngine, logger, inventory);

        var responseJson = tools.GetToolInventory(CancellationToken.None);
        using var document = JsonDocument.Parse(responseJson);
        var root = document.RootElement;
        var toolItems = root.GetProperty("tools").EnumerateArray().ToArray();

        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal("bridge_get_tool_inventory", root.GetProperty("toolName").GetString());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("requestId").GetString()));
        Assert.Equal(2, root.GetProperty("toolCount").GetInt32());
        Assert.Equal(new[] { "fake.alpha", "fake.zeta" }, toolItems.Select(tool => tool.GetProperty("id").GetString()).ToArray());

        var alphaItem = toolItems[0];
        Assert.Equal("Fake fake.alpha", alphaItem.GetProperty("name").GetString());
        Assert.Equal(BridgeToolManifest.DefaultVersion, alphaItem.GetProperty("version").GetString());
        Assert.Equal("Diagnostics", alphaItem.GetProperty("category").GetString());
        Assert.Equal("Compiled", alphaItem.GetProperty("discoveryKind").GetString());
        Assert.Equal("Compiled", alphaItem.GetProperty("source").GetString());
        Assert.Equal("SharedTests", alphaItem.GetProperty("hostAffinity").GetString());
        Assert.True(alphaItem.GetProperty("isHostSpecific").GetBoolean());
        Assert.Equal(new[] { "diagnostics.read", "workspace.read" }, alphaItem.GetProperty("requiredCapabilities").EnumerateArray().Select(value => value.GetString()).ToArray());
        Assert.Equal("Required", alphaItem.GetProperty("approvalRequirement").GetString());
        Assert.Equal("ToolExecution", alphaItem.GetProperty("auditCategoryHint").GetString());
        Assert.Equal("Informational", alphaItem.GetProperty("severityHint").GetString());
        Assert.Equal("Low", alphaItem.GetProperty("riskLevelHint").GetString());
        Assert.True(alphaItem.GetProperty("executesThroughBridgeToolExecutor").GetBoolean());
        Assert.Equal(0, alpha.ExecutionCount);
        Assert.Equal(0, zeta.ExecutionCount);
        Assert.Equal(0, pipeClient.ProposeTextEditCalls);
        Assert.Null(chatEngine.LastRequest);
        Assert.Contains(logger.InformationMessages, message => message.Contains("MCP bridge_get_tool_inventory started", StringComparison.Ordinal));
        Assert.Contains(logger.InformationMessages, message => message.Contains("ToolCount=2", StringComparison.Ordinal));
    }

    [Fact]
    public void GetToolInventory_includes_compiled_bridge_tools_from_mcp_host_services()
    {
        var services = new ServiceCollection();
        services.AddSingleton<Microsoft.Extensions.Logging.ILogger, RecordingBridgeLogger>();
        services.AddBridgeToolServices();
        using var provider = services.BuildServiceProvider();
        var tools = new VsTools(
            new RecordingPipeClient(),
            new StubChatEngine(),
            NullLogger.Instance,
            provider.GetRequiredService<IBridgeToolInventoryService>());

        var responseJson = tools.GetToolInventory(CancellationToken.None);
        using var document = JsonDocument.Parse(responseJson);
        var toolItems = document.RootElement.GetProperty("tools").EnumerateArray().ToArray();

        Assert.Contains(toolItems, tool => tool.GetProperty("id").GetString() == RegexTextSearchTool.ToolId);
        Assert.Contains(toolItems, tool => tool.GetProperty("id").GetString() == Bm25TextSearchTool.ToolId);
        Assert.Equal(toolItems.Select(tool => tool.GetProperty("id").GetString()).OrderBy(id => id, StringComparer.Ordinal), toolItems.Select(tool => tool.GetProperty("id").GetString()));
    }

    [Fact]
    public void GetToolInventory_returns_empty_snapshot_safely_when_inventory_is_not_registered()
    {
        var tools = new VsTools(new RecordingPipeClient(), new StubChatEngine(), NullLogger.Instance);

        var responseJson = tools.GetToolInventory(CancellationToken.None);
        using var document = JsonDocument.Parse(responseJson);

        Assert.True(document.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal(0, document.RootElement.GetProperty("toolCount").GetInt32());
        Assert.Empty(document.RootElement.GetProperty("tools").EnumerateArray());
    }

    [Fact]
    public void GetToolInventory_handles_missing_mef_directory_without_failing()
    {
        var missingDirectory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var services = new ServiceCollection();
        services.AddSingleton<Microsoft.Extensions.Logging.ILogger, RecordingBridgeLogger>();
        services.AddBridgeToolServices(options =>
        {
            options.EnableMefDirectoryDiscovery = true;
            options.MefDirectories.Add(missingDirectory);
        });
        using var provider = services.BuildServiceProvider();
        var tools = new VsTools(
            new RecordingPipeClient(),
            new StubChatEngine(),
            NullLogger.Instance,
            provider.GetRequiredService<IBridgeToolInventoryService>());

        var responseJson = tools.GetToolInventory(CancellationToken.None);
        using var document = JsonDocument.Parse(responseJson);
        var toolIds = document.RootElement.GetProperty("tools").EnumerateArray().Select(tool => tool.GetProperty("id").GetString()).ToArray();

        Assert.Contains(RegexTextSearchTool.ToolId, toolIds);
        Assert.Contains(Bm25TextSearchTool.ToolId, toolIds);
    }

    [Fact]
    public async Task ProposeTextEditAsync_preserves_backward_compatible_single_file_request_flow()
    {
        var pipeClient = new RecordingPipeClient();
        var tools = new VsTools(pipeClient, new StubChatEngine(), NullLogger.Instance);

        var response = await tools.ProposeTextEditAsync("sample.cs", "before", "after", CancellationToken.None);

        Assert.Equal(1, pipeClient.ProposeTextEditCalls);
        Assert.Equal(0, pipeClient.ProposeTextEditsCalls);
        Assert.Contains("Proposed diff for sample.cs", response, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProposeTextEditsAsync_sends_multi_file_request_through_pipe_client()
    {
        var pipeClient = new RecordingPipeClient();
        var tools = new VsTools(pipeClient, new StubChatEngine(), NullLogger.Instance);
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
        var tools = new VsTools(pipeClient, new StubChatEngine(), NullLogger.Instance);

        var response = await tools.ProposeTextEditsAsync(
            new[] { new ProposalFileEditRequest { FilePath = string.Empty, OriginalText = "before", ProposedText = "after" } },
            CancellationToken.None);

        Assert.Equal("Error: each file edit must include a non-empty filePath.", response);
        Assert.Equal(0, pipeClient.ProposeTextEditCalls);
        Assert.Equal(0, pipeClient.ProposeTextEditsCalls);
    }

    [Fact]
    public async Task ChatEnginePingAsync_routes_ping_through_chat_engine()
    {
        var pipeClient = new RecordingPipeClient();
        var chatEngine = new StubChatEngine();
        var tools = new VsTools(pipeClient, chatEngine, NullLogger.Instance);

        var response = await tools.ChatEnginePingAsync(CancellationToken.None);

        Assert.Equal("pong", response);
        Assert.Equal("ping", chatEngine.LastRequest?.Message);
    }

    [Fact]
    public async Task ChatEngineChatAsync_routes_input_through_chat_engine()
    {
        var pipeClient = new RecordingPipeClient();
        var chatEngine = new StubChatEngine();
        var tools = new VsTools(pipeClient, chatEngine, NullLogger.Instance);

        var responseJson = await tools.ChatEngineChatAsync("hello", CancellationToken.None);
        var response = JsonSerializer.Deserialize<ChatEngineChatResult>(responseJson);

        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.Equal("echo:hello", response.Content);
        Assert.Null(response.Error);
        Assert.Null(response.ErrorCode);
        Assert.False(string.IsNullOrWhiteSpace(response.RequestId));
        Assert.Equal("hello", chatEngine.LastRequest?.Message);
    }

    [Fact]
    public async Task ChatEngineChatAsync_returns_controlled_error_for_null_input_without_calling_chat_engine()
    {
        var pipeClient = new RecordingPipeClient();
        var chatEngine = new StubChatEngine();
        var tools = new VsTools(pipeClient, chatEngine, NullLogger.Instance);

#pragma warning disable CS8625
        var responseJson = await tools.ChatEngineChatAsync(null, CancellationToken.None);
#pragma warning restore CS8625
        var response = JsonSerializer.Deserialize<ChatEngineChatResult>(responseJson);

        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Null(response.Content);
        Assert.Equal("Error: chat_engine_chat requires a non-empty message no longer than 4000 characters.", response.Error);
        Assert.Equal("InvalidInput", response.ErrorCode);
        Assert.False(string.IsNullOrWhiteSpace(response.RequestId));
        Assert.Null(chatEngine.LastRequest);
    }

    [Fact]
    public async Task ChatEngineChatAsync_trims_success_content()
    {
        var pipeClient = new RecordingPipeClient();
        var chatEngine = new StubChatEngine("  trimmed content  \n");
        var tools = new VsTools(pipeClient, chatEngine, NullLogger.Instance);

        var responseJson = await tools.ChatEngineChatAsync("hello", CancellationToken.None);
        var response = JsonSerializer.Deserialize<ChatEngineChatResult>(responseJson);

        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.Equal("trimmed content", response.Content);
        Assert.Null(response.Error);
        Assert.Null(response.ErrorCode);
        Assert.False(string.IsNullOrWhiteSpace(response.RequestId));
    }

    [Fact]
    public async Task ChatEngineChatAsync_returns_controlled_error_for_empty_input_without_calling_chat_engine()
    {
        var pipeClient = new RecordingPipeClient();
        var chatEngine = new StubChatEngine();
        var tools = new VsTools(pipeClient, chatEngine, NullLogger.Instance);

        var responseJson = await tools.ChatEngineChatAsync(string.Empty, CancellationToken.None);
        var response = JsonSerializer.Deserialize<ChatEngineChatResult>(responseJson);

        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Null(response.Content);
        Assert.Equal("Error: chat_engine_chat requires a non-empty message no longer than 4000 characters.", response.Error);
        Assert.Equal("InvalidInput", response.ErrorCode);
        Assert.False(string.IsNullOrWhiteSpace(response.RequestId));
        Assert.Null(chatEngine.LastRequest);
    }

    [Fact]
    public async Task ChatEngineChatAsync_returns_controlled_error_for_whitespace_input_without_calling_chat_engine()
    {
        var pipeClient = new RecordingPipeClient();
        var chatEngine = new StubChatEngine();
        var tools = new VsTools(pipeClient, chatEngine, NullLogger.Instance);

        var responseJson = await tools.ChatEngineChatAsync("   ", CancellationToken.None);
        var response = JsonSerializer.Deserialize<ChatEngineChatResult>(responseJson);

        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Null(response.Content);
        Assert.Equal("Error: chat_engine_chat requires a non-empty message no longer than 4000 characters.", response.Error);
        Assert.Equal("InvalidInput", response.ErrorCode);
        Assert.False(string.IsNullOrWhiteSpace(response.RequestId));
        Assert.Null(chatEngine.LastRequest);
    }

    [Fact]
    public async Task ChatEngineChatAsync_returns_controlled_error_for_oversized_input_without_calling_chat_engine()
    {
        var pipeClient = new RecordingPipeClient();
        var chatEngine = new StubChatEngine();
        var tools = new VsTools(pipeClient, chatEngine, NullLogger.Instance);

        var responseJson = await tools.ChatEngineChatAsync(new string('x', 4001), CancellationToken.None);
        var response = JsonSerializer.Deserialize<ChatEngineChatResult>(responseJson);

        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Null(response.Content);
        Assert.Equal("Error: chat_engine_chat requires a non-empty message no longer than 4000 characters.", response.Error);
        Assert.Equal("InvalidInput", response.ErrorCode);
        Assert.False(string.IsNullOrWhiteSpace(response.RequestId));
        Assert.Null(chatEngine.LastRequest);
    }

    [Fact]
    public async Task ChatEngineChatAsync_returns_controlled_error_when_chat_engine_fails()
    {
        var pipeClient = new RecordingPipeClient();
        var chatEngine = new ThrowingChatEngine();
        var tools = new VsTools(pipeClient, chatEngine, NullLogger.Instance);

        var responseJson = await tools.ChatEngineChatAsync("hello", CancellationToken.None);
        var response = JsonSerializer.Deserialize<ChatEngineChatResult>(responseJson);

        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Null(response.Content);
        Assert.Equal("Error: chat_engine_chat failed.", response.Error);
        Assert.Equal("ProviderFailure", response.ErrorCode);
        Assert.False(string.IsNullOrWhiteSpace(response.RequestId));
        Assert.Equal("hello", chatEngine.LastRequest?.Message);
    }

    [Fact]
    public async Task ChatEngineSummarizeAsync_returns_success_with_content_for_valid_input()
    {
        var pipeClient = new RecordingPipeClient();
        var chatEngine = new StubChatEngine("summary");
        var tools = new VsTools(pipeClient, chatEngine, NullLogger.Instance);

        var responseJson = await tools.ChatEngineSummarizeAsync("some text", null, CancellationToken.None);
        var response = JsonSerializer.Deserialize<ChatEngineChatResult>(responseJson);

        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.Equal("summary", response.Content);
        Assert.Null(response.Error);
        Assert.Null(response.ErrorCode);
        Assert.False(string.IsNullOrWhiteSpace(response.RequestId));
        Assert.Equal("Summarize the following text:\n\nsome text", chatEngine.LastRequest?.Message);
    }

    [Fact]
    public async Task ChatEngineSummarizeAsync_includes_valid_max_length_in_prompt()
    {
        var pipeClient = new RecordingPipeClient();
        var chatEngine = new StubChatEngine("summary");
        var tools = new VsTools(pipeClient, chatEngine, NullLogger.Instance);

        var responseJson = await tools.ChatEngineSummarizeAsync("some text", 25, CancellationToken.None);
        var response = JsonSerializer.Deserialize<ChatEngineChatResult>(responseJson);

        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.Equal("summary", response.Content);
        Assert.Null(response.Error);
        Assert.Null(response.ErrorCode);
        Assert.False(string.IsNullOrWhiteSpace(response.RequestId));
        Assert.Equal("Summarize the following text in no more than 25 words:\n\nsome text", chatEngine.LastRequest?.Message);
    }

    [Fact]
    public async Task ChatEngineSummarizeAsync_returns_invalid_input_for_empty_input_without_calling_chat_engine()
    {
        var pipeClient = new RecordingPipeClient();
        var chatEngine = new StubChatEngine();
        var tools = new VsTools(pipeClient, chatEngine, NullLogger.Instance);

        var responseJson = await tools.ChatEngineSummarizeAsync(string.Empty, null, CancellationToken.None);
        var response = JsonSerializer.Deserialize<ChatEngineChatResult>(responseJson);

        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Null(response.Content);
        Assert.Equal("Error: chat_engine_summarize requires a non-empty message no longer than 4000 characters.", response.Error);
        Assert.Equal("InvalidInput", response.ErrorCode);
        Assert.False(string.IsNullOrWhiteSpace(response.RequestId));
        Assert.Null(chatEngine.LastRequest);
    }

    [Fact]
    public async Task ChatEngineSummarizeAsync_returns_invalid_input_for_invalid_max_length_without_calling_chat_engine()
    {
        var pipeClient = new RecordingPipeClient();
        var chatEngine = new StubChatEngine();
        var tools = new VsTools(pipeClient, chatEngine, NullLogger.Instance);

        var responseJson = await tools.ChatEngineSummarizeAsync("some text", 0, CancellationToken.None);
        var response = JsonSerializer.Deserialize<ChatEngineChatResult>(responseJson);

        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Null(response.Content);
        Assert.Equal("Error: chat_engine_summarize max_length must be greater than 0 and less than or equal to 1000.", response.Error);
        Assert.Equal("InvalidInput", response.ErrorCode);
        Assert.False(string.IsNullOrWhiteSpace(response.RequestId));
        Assert.Null(chatEngine.LastRequest);
    }

    [Fact]
    public async Task ChatEngineSummarizeAsync_returns_provider_failure_when_chat_engine_fails()
    {
        var pipeClient = new RecordingPipeClient();
        var chatEngine = new ThrowingChatEngine();
        var tools = new VsTools(pipeClient, chatEngine, NullLogger.Instance);

        var responseJson = await tools.ChatEngineSummarizeAsync("some text", null, CancellationToken.None);
        var response = JsonSerializer.Deserialize<ChatEngineChatResult>(responseJson);

        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Null(response.Content);
        Assert.Equal("Error: chat_engine_summarize failed.", response.Error);
        Assert.Equal("ProviderFailure", response.ErrorCode);
        Assert.False(string.IsNullOrWhiteSpace(response.RequestId));
        Assert.Equal("Summarize the following text:\n\nsome text", chatEngine.LastRequest?.Message);
    }

    [Fact]
    public async Task ChatEngineRewriteAsync_returns_success_with_content_for_valid_input()
    {
        var pipeClient = new RecordingPipeClient();
        var chatEngine = new StubChatEngine("rewrite");
        var tools = new VsTools(pipeClient, chatEngine, NullLogger.Instance);

        var responseJson = await tools.ChatEngineRewriteAsync("some text", null, CancellationToken.None);
        var response = JsonSerializer.Deserialize<ChatEngineChatResult>(responseJson);

        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.Equal("rewrite", response.Content);
        Assert.Null(response.Error);
        Assert.Null(response.ErrorCode);
        Assert.False(string.IsNullOrWhiteSpace(response.RequestId));
        Assert.Equal("Rewrite the following text to be clearer and more concise:\n\nsome text", chatEngine.LastRequest?.Message);
    }

    [Fact]
    public async Task ChatEngineRewriteAsync_includes_valid_tone_in_prompt()
    {
        var pipeClient = new RecordingPipeClient();
        var chatEngine = new StubChatEngine("rewrite");
        var tools = new VsTools(pipeClient, chatEngine, NullLogger.Instance);

        var responseJson = await tools.ChatEngineRewriteAsync("some text", "technical", CancellationToken.None);
        var response = JsonSerializer.Deserialize<ChatEngineChatResult>(responseJson);

        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.Equal("rewrite", response.Content);
        Assert.Null(response.Error);
        Assert.Null(response.ErrorCode);
        Assert.False(string.IsNullOrWhiteSpace(response.RequestId));
        Assert.Equal("Rewrite the following text to be clearer and more concise in a technical tone:\n\nsome text", chatEngine.LastRequest?.Message);
    }

    [Fact]
    public async Task ChatEngineRewriteAsync_returns_invalid_input_for_empty_input_without_calling_chat_engine()
    {
        var pipeClient = new RecordingPipeClient();
        var chatEngine = new StubChatEngine();
        var tools = new VsTools(pipeClient, chatEngine, NullLogger.Instance);

        var responseJson = await tools.ChatEngineRewriteAsync(string.Empty, null, CancellationToken.None);
        var response = JsonSerializer.Deserialize<ChatEngineChatResult>(responseJson);

        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Null(response.Content);
        Assert.Equal("Error: chat_engine_rewrite requires a non-empty message no longer than 4000 characters.", response.Error);
        Assert.Equal("InvalidInput", response.ErrorCode);
        Assert.False(string.IsNullOrWhiteSpace(response.RequestId));
        Assert.Null(chatEngine.LastRequest);
    }

    [Fact]
    public async Task ChatEngineRewriteAsync_returns_invalid_input_for_invalid_tone_without_calling_chat_engine()
    {
        var pipeClient = new RecordingPipeClient();
        var chatEngine = new StubChatEngine();
        var tools = new VsTools(pipeClient, chatEngine, NullLogger.Instance);

        var responseJson = await tools.ChatEngineRewriteAsync("some text", "playful", CancellationToken.None);
        var response = JsonSerializer.Deserialize<ChatEngineChatResult>(responseJson);

        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Null(response.Content);
        Assert.Equal("Error: chat_engine_rewrite tone must be one of: formal, casual, technical.", response.Error);
        Assert.Equal("InvalidInput", response.ErrorCode);
        Assert.False(string.IsNullOrWhiteSpace(response.RequestId));
        Assert.Null(chatEngine.LastRequest);
    }

    [Fact]
    public async Task ChatEngineRewriteAsync_returns_provider_failure_when_chat_engine_fails()
    {
        var pipeClient = new RecordingPipeClient();
        var chatEngine = new ThrowingChatEngine();
        var tools = new VsTools(pipeClient, chatEngine, NullLogger.Instance);

        var responseJson = await tools.ChatEngineRewriteAsync("some text", null, CancellationToken.None);
        var response = JsonSerializer.Deserialize<ChatEngineChatResult>(responseJson);

        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Null(response.Content);
        Assert.Equal("Error: chat_engine_rewrite failed.", response.Error);
        Assert.Equal("ProviderFailure", response.ErrorCode);
        Assert.False(string.IsNullOrWhiteSpace(response.RequestId));
        Assert.Equal("Rewrite the following text to be clearer and more concise:\n\nsome text", chatEngine.LastRequest?.Message);
    }

    [Fact]
    public async Task ChatEngineRewriteWithTargetAsync_creates_proposal_for_valid_input()
    {
        var pipeClient = new RecordingPipeClient();
        var chatEngine = new StubChatEngine("rewritten text");
        var tools = new VsTools(pipeClient, chatEngine, NullLogger.Instance);

        var responseJson = await tools.ChatEngineRewriteWithTargetAsync("sample.cs", "original text", null, CancellationToken.None);
        var response = JsonSerializer.Deserialize<ChatEngineChatResult>(responseJson);

        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.Equal("Proposal created", response.Content);
        Assert.Null(response.Error);
        Assert.Null(response.ErrorCode);
        Assert.False(string.IsNullOrWhiteSpace(response.RequestId));
        Assert.Equal("Rewrite the following text to be clearer and more concise:\n\noriginal text", chatEngine.LastRequest?.Message);
        Assert.Equal(1, pipeClient.ProposeTextEditCalls);
        Assert.Equal("sample.cs", pipeClient.LastProposedFilePath);
        Assert.Equal("original text", pipeClient.LastProposedOriginalText);
        Assert.Equal("rewritten text", pipeClient.LastProposedText);
    }

    [Fact]
    public async Task ChatEngineRewriteWithTargetAsync_requires_explicit_file_path()
    {
        var pipeClient = new RecordingPipeClient();
        var chatEngine = new StubChatEngine();
        var tools = new VsTools(pipeClient, chatEngine, NullLogger.Instance);

        var responseJson = await tools.ChatEngineRewriteWithTargetAsync("   ", "original text", null, CancellationToken.None);
        var response = JsonSerializer.Deserialize<ChatEngineChatResult>(responseJson);

        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Equal("InvalidInput", response.ErrorCode);
        Assert.Null(chatEngine.LastRequest);
        Assert.Equal(0, pipeClient.ProposeTextEditCalls);
    }

    [Fact]
    public async Task ChatEngineRewriteWithTargetAsync_requires_explicit_original_text()
    {
        var pipeClient = new RecordingPipeClient();
        var chatEngine = new StubChatEngine();
        var tools = new VsTools(pipeClient, chatEngine, NullLogger.Instance);

        var responseJson = await tools.ChatEngineRewriteWithTargetAsync("sample.cs", "", null, CancellationToken.None);
        var response = JsonSerializer.Deserialize<ChatEngineChatResult>(responseJson);

        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Equal("InvalidInput", response.ErrorCode);
        Assert.Null(chatEngine.LastRequest);
        Assert.Equal(0, pipeClient.ProposeTextEditCalls);
    }

    [Fact]
    public async Task ChatEngineRewriteWithTargetAsync_returns_invalid_input_without_creating_proposal()
    {
        var pipeClient = new RecordingPipeClient();
        var chatEngine = new StubChatEngine();
        var tools = new VsTools(pipeClient, chatEngine, NullLogger.Instance);

        var responseJson = await tools.ChatEngineRewriteWithTargetAsync(string.Empty, "original text", null, CancellationToken.None);
        var response = JsonSerializer.Deserialize<ChatEngineChatResult>(responseJson);

        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Null(response.Content);
        Assert.Equal("Error: chat_engine_rewrite_with_target requires a non-empty filePath and originalText.", response.Error);
        Assert.Equal("InvalidInput", response.ErrorCode);
        Assert.False(string.IsNullOrWhiteSpace(response.RequestId));
        Assert.Null(chatEngine.LastRequest);
        Assert.Equal(0, pipeClient.ProposeTextEditCalls);
    }

    [Fact]
    public async Task ChatEngineRewriteWithTargetAsync_invalid_input_never_calls_chat_engine_or_proposal_api()
    {
        var pipeClient = new RecordingPipeClient();
        var chatEngine = new TrackingChatEngine("rewritten text");
        var tools = new VsTools(pipeClient, chatEngine, NullLogger.Instance);

        var responseJson = await tools.ChatEngineRewriteWithTargetAsync("", "", null, CancellationToken.None);
        var response = JsonSerializer.Deserialize<ChatEngineChatResult>(responseJson);

        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Equal("InvalidInput", response.ErrorCode);
        Assert.Equal(0, chatEngine.SendAsyncCalls);
        Assert.Equal(0, chatEngine.StreamAsyncCalls);
        Assert.Equal(0, pipeClient.ProposeTextEditCalls);
    }

    [Fact]
    public async Task ChatEngineRewriteWithTargetAsync_returns_provider_failure_without_creating_proposal()
    {
        var pipeClient = new RecordingPipeClient();
        var chatEngine = new ThrowingChatEngine();
        var tools = new VsTools(pipeClient, chatEngine, NullLogger.Instance);

        var responseJson = await tools.ChatEngineRewriteWithTargetAsync("sample.cs", "original text", null, CancellationToken.None);
        var response = JsonSerializer.Deserialize<ChatEngineChatResult>(responseJson);

        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Null(response.Content);
        Assert.Equal("Error: chat_engine_rewrite_with_target failed.", response.Error);
        Assert.Equal("ProviderFailure", response.ErrorCode);
        Assert.False(string.IsNullOrWhiteSpace(response.RequestId));
        Assert.Equal("Rewrite the following text to be clearer and more concise:\n\noriginal text", chatEngine.LastRequest?.Message);
        Assert.Equal(0, pipeClient.ProposeTextEditCalls);
    }

    [Fact]
    public async Task ChatEngineRewriteWithTargetAsync_success_path_calls_proposal_api_exactly_once_and_never_apply_directly()
    {
        var pipeClient = new RecordingPipeClient();
        var chatEngine = new TrackingChatEngine("rewritten text");
        var tools = new VsTools(pipeClient, chatEngine, NullLogger.Instance);

        var responseJson = await tools.ChatEngineRewriteWithTargetAsync("sample.cs", "original text", null, CancellationToken.None);
        var response = JsonSerializer.Deserialize<ChatEngineChatResult>(responseJson);

        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.Equal(1, chatEngine.SendAsyncCalls);
        Assert.Equal(0, chatEngine.StreamAsyncCalls);
        Assert.Equal(1, pipeClient.ProposeTextEditCalls);
        Assert.Equal(0, pipeClient.ProposeTextEditsCalls);
    }

    [Fact]
    public async Task ChatEngineSuggestFixesAsync_returns_success_with_content_for_valid_input()
    {
        var pipeClient = new RecordingPipeClient();
        var chatEngine = new StubChatEngine("suggestions");
        var tools = new VsTools(pipeClient, chatEngine, NullLogger.Instance);

        var responseJson = await tools.ChatEngineSuggestFixesAsync("some text", CancellationToken.None);
        var response = JsonSerializer.Deserialize<ChatEngineChatResult>(responseJson);

        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.Equal("suggestions", response.Content);
        Assert.Null(response.Error);
        Assert.Null(response.ErrorCode);
        Assert.False(string.IsNullOrWhiteSpace(response.RequestId));
        Assert.Equal("Review the following text and suggest improvements or fixes:\n\nsome text", chatEngine.LastRequest?.Message);
    }

    [Fact]
    public async Task ChatEngineSuggestFixesAsync_returns_invalid_input_for_empty_input_without_calling_chat_engine()
    {
        var pipeClient = new RecordingPipeClient();
        var chatEngine = new StubChatEngine();
        var tools = new VsTools(pipeClient, chatEngine, NullLogger.Instance);

        var responseJson = await tools.ChatEngineSuggestFixesAsync(string.Empty, CancellationToken.None);
        var response = JsonSerializer.Deserialize<ChatEngineChatResult>(responseJson);

        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Null(response.Content);
        Assert.Equal("Error: chat_engine_suggest_fixes requires a non-empty message no longer than 4000 characters.", response.Error);
        Assert.Equal("InvalidInput", response.ErrorCode);
        Assert.False(string.IsNullOrWhiteSpace(response.RequestId));
        Assert.Null(chatEngine.LastRequest);
    }

    [Fact]
    public async Task ChatEngineSuggestFixesAsync_returns_provider_failure_when_chat_engine_fails()
    {
        var pipeClient = new RecordingPipeClient();
        var chatEngine = new ThrowingChatEngine();
        var tools = new VsTools(pipeClient, chatEngine, NullLogger.Instance);

        var responseJson = await tools.ChatEngineSuggestFixesAsync("some text", CancellationToken.None);
        var response = JsonSerializer.Deserialize<ChatEngineChatResult>(responseJson);

        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Null(response.Content);
        Assert.Equal("Error: chat_engine_suggest_fixes failed.", response.Error);
        Assert.Equal("ProviderFailure", response.ErrorCode);
        Assert.False(string.IsNullOrWhiteSpace(response.RequestId));
        Assert.Equal("Review the following text and suggest improvements or fixes:\n\nsome text", chatEngine.LastRequest?.Message);
    }

    [Fact]
    public async Task ChatEngineSuggestFixesWithTargetAsync_creates_proposal_for_valid_input()
    {
        var pipeClient = new RecordingPipeClient();
        var chatEngine = new StubChatEngine("suggested fixes");
        var tools = new VsTools(pipeClient, chatEngine, NullLogger.Instance);

        var responseJson = await tools.ChatEngineSuggestFixesWithTargetAsync("sample.cs", "original text", CancellationToken.None);
        var response = JsonSerializer.Deserialize<ChatEngineChatResult>(responseJson);

        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.Equal("Proposal created", response.Content);
        Assert.Null(response.Error);
        Assert.Null(response.ErrorCode);
        Assert.False(string.IsNullOrWhiteSpace(response.RequestId));
        Assert.Equal("Review the following text and suggest improvements or fixes:\n\noriginal text", chatEngine.LastRequest?.Message);
        Assert.Equal(1, pipeClient.ProposeTextEditCalls);
        Assert.Equal("sample.cs", pipeClient.LastProposedFilePath);
        Assert.Equal("original text", pipeClient.LastProposedOriginalText);
        Assert.Equal("suggested fixes", pipeClient.LastProposedText);
    }

    [Fact]
    public async Task ChatEngineSuggestFixesWithTargetAsync_requires_explicit_file_path()
    {
        var pipeClient = new RecordingPipeClient();
        var chatEngine = new StubChatEngine();
        var tools = new VsTools(pipeClient, chatEngine, NullLogger.Instance);

        var responseJson = await tools.ChatEngineSuggestFixesWithTargetAsync("   ", "original text", CancellationToken.None);
        var response = JsonSerializer.Deserialize<ChatEngineChatResult>(responseJson);

        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Equal("InvalidInput", response.ErrorCode);
        Assert.Null(chatEngine.LastRequest);
        Assert.Equal(0, pipeClient.ProposeTextEditCalls);
    }

    [Fact]
    public async Task ChatEngineSuggestFixesWithTargetAsync_requires_explicit_original_text()
    {
        var pipeClient = new RecordingPipeClient();
        var chatEngine = new StubChatEngine();
        var tools = new VsTools(pipeClient, chatEngine, NullLogger.Instance);

        var responseJson = await tools.ChatEngineSuggestFixesWithTargetAsync("sample.cs", "", CancellationToken.None);
        var response = JsonSerializer.Deserialize<ChatEngineChatResult>(responseJson);

        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Equal("InvalidInput", response.ErrorCode);
        Assert.Null(chatEngine.LastRequest);
        Assert.Equal(0, pipeClient.ProposeTextEditCalls);
    }

    [Fact]
    public async Task ChatEngineSuggestFixesWithTargetAsync_invalid_input_never_calls_chat_engine_or_proposal_api()
    {
        var pipeClient = new RecordingPipeClient();
        var chatEngine = new TrackingChatEngine("suggested fixes");
        var tools = new VsTools(pipeClient, chatEngine, NullLogger.Instance);

        var responseJson = await tools.ChatEngineSuggestFixesWithTargetAsync("", "", CancellationToken.None);
        var response = JsonSerializer.Deserialize<ChatEngineChatResult>(responseJson);

        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Equal("InvalidInput", response.ErrorCode);
        Assert.Equal(0, chatEngine.SendAsyncCalls);
        Assert.Equal(0, chatEngine.StreamAsyncCalls);
        Assert.Equal(0, pipeClient.ProposeTextEditCalls);
    }

    [Fact]
    public async Task ChatEngineSuggestFixesWithTargetAsync_returns_provider_failure_without_creating_proposal()
    {
        var pipeClient = new RecordingPipeClient();
        var chatEngine = new ThrowingChatEngine();
        var tools = new VsTools(pipeClient, chatEngine, NullLogger.Instance);

        var responseJson = await tools.ChatEngineSuggestFixesWithTargetAsync("sample.cs", "original text", CancellationToken.None);
        var response = JsonSerializer.Deserialize<ChatEngineChatResult>(responseJson);

        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Null(response.Content);
        Assert.Equal("Error: chat_engine_suggest_fixes_with_target failed.", response.Error);
        Assert.Equal("ProviderFailure", response.ErrorCode);
        Assert.False(string.IsNullOrWhiteSpace(response.RequestId));
        Assert.Equal("Review the following text and suggest improvements or fixes:\n\noriginal text", chatEngine.LastRequest?.Message);
        Assert.Equal(0, pipeClient.ProposeTextEditCalls);
    }

    [Fact]
    public async Task ChatEngineSuggestFixesWithTargetAsync_success_path_calls_proposal_api_exactly_once_and_never_apply_directly()
    {
        var pipeClient = new RecordingPipeClient();
        var chatEngine = new TrackingChatEngine("suggested fixes");
        var tools = new VsTools(pipeClient, chatEngine, NullLogger.Instance);

        var responseJson = await tools.ChatEngineSuggestFixesWithTargetAsync("sample.cs", "original text", CancellationToken.None);
        var response = JsonSerializer.Deserialize<ChatEngineChatResult>(responseJson);

        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.Equal(1, chatEngine.SendAsyncCalls);
        Assert.Equal(0, chatEngine.StreamAsyncCalls);
        Assert.Equal(1, pipeClient.ProposeTextEditCalls);
        Assert.Equal(0, pipeClient.ProposeTextEditsCalls);
    }

    [Fact]
    public async Task ChatEngineExplainCodeAsync_returns_success_with_content_for_valid_input()
    {
        var pipeClient = new RecordingPipeClient();
        var chatEngine = new StubChatEngine("explanation");
        var tools = new VsTools(pipeClient, chatEngine, NullLogger.Instance);

        var responseJson = await tools.ChatEngineExplainCodeAsync("var x = 1;", CancellationToken.None);
        var response = JsonSerializer.Deserialize<ChatEngineChatResult>(responseJson);

        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.Equal("explanation", response.Content);
        Assert.Null(response.Error);
        Assert.Null(response.ErrorCode);
        Assert.False(string.IsNullOrWhiteSpace(response.RequestId));
        Assert.Equal("Explain the following code clearly and concisely:\n\nvar x = 1;", chatEngine.LastRequest?.Message);
    }

    [Fact]
    public async Task ChatEngineExplainCodeAsync_returns_invalid_input_for_empty_input_without_calling_chat_engine()
    {
        var pipeClient = new RecordingPipeClient();
        var chatEngine = new StubChatEngine();
        var tools = new VsTools(pipeClient, chatEngine, NullLogger.Instance);

        var responseJson = await tools.ChatEngineExplainCodeAsync(string.Empty, CancellationToken.None);
        var response = JsonSerializer.Deserialize<ChatEngineChatResult>(responseJson);

        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Null(response.Content);
        Assert.Equal("Error: chat_engine_explain_code requires a non-empty message no longer than 4000 characters.", response.Error);
        Assert.Equal("InvalidInput", response.ErrorCode);
        Assert.False(string.IsNullOrWhiteSpace(response.RequestId));
        Assert.Null(chatEngine.LastRequest);
    }

    [Fact]
    public async Task ChatEngineExplainCodeAsync_returns_provider_failure_when_chat_engine_fails()
    {
        var pipeClient = new RecordingPipeClient();
        var chatEngine = new ThrowingChatEngine();
        var tools = new VsTools(pipeClient, chatEngine, NullLogger.Instance);

        var responseJson = await tools.ChatEngineExplainCodeAsync("var x = 1;", CancellationToken.None);
        var response = JsonSerializer.Deserialize<ChatEngineChatResult>(responseJson);

        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Null(response.Content);
        Assert.Equal("Error: chat_engine_explain_code failed.", response.Error);
        Assert.Equal("ProviderFailure", response.ErrorCode);
        Assert.False(string.IsNullOrWhiteSpace(response.RequestId));
        Assert.Equal("Explain the following code clearly and concisely:\n\nvar x = 1;", chatEngine.LastRequest?.Message);
    }

    [Fact]
    public async Task ChatEngineExplainErrorAsync_returns_success_with_content_for_valid_input()
    {
        var pipeClient = new RecordingPipeClient();
        var chatEngine = new StubChatEngine("diagnostic explanation");
        var tools = new VsTools(pipeClient, chatEngine, NullLogger.Instance);

        var responseJson = await tools.ChatEngineExplainErrorAsync("CS1002 ; expected", CancellationToken.None);
        var response = JsonSerializer.Deserialize<ChatEngineChatResult>(responseJson);

        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.Equal("diagnostic explanation", response.Content);
        Assert.Null(response.Error);
        Assert.Null(response.ErrorCode);
        Assert.False(string.IsNullOrWhiteSpace(response.RequestId));
        Assert.Equal("Explain the following compiler or diagnostic error and suggest the likely cause:\n\nCS1002 ; expected", chatEngine.LastRequest?.Message);
    }

    [Fact]
    public async Task ChatEngineExplainErrorAsync_returns_invalid_input_for_empty_input_without_calling_chat_engine()
    {
        var pipeClient = new RecordingPipeClient();
        var chatEngine = new StubChatEngine();
        var tools = new VsTools(pipeClient, chatEngine, NullLogger.Instance);

        var responseJson = await tools.ChatEngineExplainErrorAsync(string.Empty, CancellationToken.None);
        var response = JsonSerializer.Deserialize<ChatEngineChatResult>(responseJson);

        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Null(response.Content);
        Assert.Equal("Error: chat_engine_explain_error requires a non-empty message no longer than 4000 characters.", response.Error);
        Assert.Equal("InvalidInput", response.ErrorCode);
        Assert.False(string.IsNullOrWhiteSpace(response.RequestId));
        Assert.Null(chatEngine.LastRequest);
    }

    [Fact]
    public async Task ChatEngineExplainErrorAsync_returns_provider_failure_when_chat_engine_fails()
    {
        var pipeClient = new RecordingPipeClient();
        var chatEngine = new ThrowingChatEngine();
        var tools = new VsTools(pipeClient, chatEngine, NullLogger.Instance);

        var responseJson = await tools.ChatEngineExplainErrorAsync("CS1002 ; expected", CancellationToken.None);
        var response = JsonSerializer.Deserialize<ChatEngineChatResult>(responseJson);

        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Null(response.Content);
        Assert.Equal("Error: chat_engine_explain_error failed.", response.Error);
        Assert.Equal("ProviderFailure", response.ErrorCode);
        Assert.False(string.IsNullOrWhiteSpace(response.RequestId));
        Assert.Equal("Explain the following compiler or diagnostic error and suggest the likely cause:\n\nCS1002 ; expected", chatEngine.LastRequest?.Message);
    }

    [Fact]
    public async Task ChatEngineSuggestErrorFixAsync_returns_success_with_content_for_valid_input()
    {
        var pipeClient = new RecordingPipeClient();
        var chatEngine = new StubChatEngine("likely fix");
        var tools = new VsTools(pipeClient, chatEngine, NullLogger.Instance);

        var responseJson = await tools.ChatEngineSuggestErrorFixAsync("CS1002 ; expected", CancellationToken.None);
        var response = JsonSerializer.Deserialize<ChatEngineChatResult>(responseJson);

        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.Equal("likely fix", response.Content);
        Assert.Null(response.Error);
        Assert.Null(response.ErrorCode);
        Assert.False(string.IsNullOrWhiteSpace(response.RequestId));
        Assert.Equal("Given the following compiler or diagnostic error, suggest a likely fix and explain why:\n\nCS1002 ; expected", chatEngine.LastRequest?.Message);
    }

    [Fact]
    public async Task ChatEngineSuggestErrorFixAsync_returns_invalid_input_for_empty_input_without_calling_chat_engine()
    {
        var pipeClient = new RecordingPipeClient();
        var chatEngine = new StubChatEngine();
        var tools = new VsTools(pipeClient, chatEngine, NullLogger.Instance);

        var responseJson = await tools.ChatEngineSuggestErrorFixAsync(string.Empty, CancellationToken.None);
        var response = JsonSerializer.Deserialize<ChatEngineChatResult>(responseJson);

        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Null(response.Content);
        Assert.Equal("Error: chat_engine_suggest_error_fix requires a non-empty message no longer than 4000 characters.", response.Error);
        Assert.Equal("InvalidInput", response.ErrorCode);
        Assert.False(string.IsNullOrWhiteSpace(response.RequestId));
        Assert.Null(chatEngine.LastRequest);
    }

    [Fact]
    public async Task ChatEngineSuggestErrorFixAsync_returns_provider_failure_when_chat_engine_fails()
    {
        var pipeClient = new RecordingPipeClient();
        var chatEngine = new ThrowingChatEngine();
        var tools = new VsTools(pipeClient, chatEngine, NullLogger.Instance);

        var responseJson = await tools.ChatEngineSuggestErrorFixAsync("CS1002 ; expected", CancellationToken.None);
        var response = JsonSerializer.Deserialize<ChatEngineChatResult>(responseJson);

        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Null(response.Content);
        Assert.Equal("Error: chat_engine_suggest_error_fix failed.", response.Error);
        Assert.Equal("ProviderFailure", response.ErrorCode);
        Assert.False(string.IsNullOrWhiteSpace(response.RequestId));
        Assert.Equal("Given the following compiler or diagnostic error, suggest a likely fix and explain why:\n\nCS1002 ; expected", chatEngine.LastRequest?.Message);
    }

    [Fact]
    public async Task ChatEngineTools_use_only_SendAsync_and_never_StreamAsync()
    {
        var pipeClient = new RecordingPipeClient();
        var chatEngine = new TrackingChatEngine("result");
        var tools = new VsTools(pipeClient, chatEngine, NullLogger.Instance);

        await tools.ChatEngineChatAsync("hello", CancellationToken.None);
        await tools.ChatEngineSummarizeAsync("hello", null, CancellationToken.None);
        await tools.ChatEngineRewriteAsync("hello", null, CancellationToken.None);
        await tools.ChatEngineSuggestFixesAsync("hello", CancellationToken.None);
        await tools.ChatEngineExplainCodeAsync("hello", CancellationToken.None);
        await tools.ChatEngineExplainErrorAsync("hello", CancellationToken.None);
        await tools.ChatEngineSuggestErrorFixAsync("hello", CancellationToken.None);

        Assert.Equal(7, chatEngine.SendAsyncCalls);
        Assert.Equal(0, chatEngine.StreamAsyncCalls);
    }

    [Fact]
    public async Task ChatEngineTools_return_ChatEngineChatResult_json_not_raw_strings()
    {
        var pipeClient = new RecordingPipeClient();
        var chatEngine = new TrackingChatEngine("result");
        var tools = new VsTools(pipeClient, chatEngine, NullLogger.Instance);

        var chatResponseJson = await tools.ChatEngineChatAsync("hello", CancellationToken.None);
        var summarizeResponseJson = await tools.ChatEngineSummarizeAsync("hello", null, CancellationToken.None);
        var rewriteResponseJson = await tools.ChatEngineRewriteAsync("hello", null, CancellationToken.None);
        var suggestFixesResponseJson = await tools.ChatEngineSuggestFixesAsync("hello", CancellationToken.None);
        var explainCodeResponseJson = await tools.ChatEngineExplainCodeAsync("hello", CancellationToken.None);
        var explainErrorResponseJson = await tools.ChatEngineExplainErrorAsync("hello", CancellationToken.None);
        var suggestErrorFixResponseJson = await tools.ChatEngineSuggestErrorFixAsync("hello", CancellationToken.None);

        AssertChatEngineChatResultJson(chatResponseJson, expectedContent: "result");
        AssertChatEngineChatResultJson(summarizeResponseJson, expectedContent: "result");
        AssertChatEngineChatResultJson(rewriteResponseJson, expectedContent: "result");
        AssertChatEngineChatResultJson(suggestFixesResponseJson, expectedContent: "result");
        AssertChatEngineChatResultJson(explainCodeResponseJson, expectedContent: "result");
        AssertChatEngineChatResultJson(explainErrorResponseJson, expectedContent: "result");
        AssertChatEngineChatResultJson(suggestErrorFixResponseJson, expectedContent: "result");
    }

    [Fact]
    public async Task ChatEngineTools_use_ErrorCode_on_failure()
    {
        var pipeClient = new RecordingPipeClient();
        var chatEngine = new ThrowingChatEngine();
        var tools = new VsTools(pipeClient, chatEngine, NullLogger.Instance);

        var chatResponseJson = await tools.ChatEngineChatAsync("hello", CancellationToken.None);
        var summarizeResponseJson = await tools.ChatEngineSummarizeAsync("hello", null, CancellationToken.None);
        var rewriteResponseJson = await tools.ChatEngineRewriteAsync("hello", null, CancellationToken.None);
        var suggestFixesResponseJson = await tools.ChatEngineSuggestFixesAsync("hello", CancellationToken.None);
        var explainCodeResponseJson = await tools.ChatEngineExplainCodeAsync("hello", CancellationToken.None);
        var explainErrorResponseJson = await tools.ChatEngineExplainErrorAsync("hello", CancellationToken.None);
        var suggestErrorFixResponseJson = await tools.ChatEngineSuggestErrorFixAsync("hello", CancellationToken.None);

        AssertFailureErrorCode(chatResponseJson, "ProviderFailure");
        AssertFailureErrorCode(summarizeResponseJson, "ProviderFailure");
        AssertFailureErrorCode(rewriteResponseJson, "ProviderFailure");
        AssertFailureErrorCode(suggestFixesResponseJson, "ProviderFailure");
        AssertFailureErrorCode(explainCodeResponseJson, "ProviderFailure");
        AssertFailureErrorCode(explainErrorResponseJson, "ProviderFailure");
        AssertFailureErrorCode(suggestErrorFixResponseJson, "ProviderFailure");
    }

    [Fact]
    public async Task ChatEngineTools_do_not_call_proposal_or_apply_pipe_apis()
    {
        var pipeClient = new RecordingPipeClient();
        var chatEngine = new TrackingChatEngine("result");
        var tools = new VsTools(pipeClient, chatEngine, NullLogger.Instance);

        await tools.ChatEngineChatAsync("hello", CancellationToken.None);
        await tools.ChatEngineSummarizeAsync("hello", null, CancellationToken.None);
        await tools.ChatEngineRewriteAsync("hello", null, CancellationToken.None);
        await tools.ChatEngineSuggestFixesAsync("hello", CancellationToken.None);
        await tools.ChatEngineExplainCodeAsync("hello", CancellationToken.None);
        await tools.ChatEngineExplainErrorAsync("hello", CancellationToken.None);
        await tools.ChatEngineSuggestErrorFixAsync("hello", CancellationToken.None);
        await tools.ChatEngineRewriteWithTargetAsync("sample.cs", "hello", null, CancellationToken.None);
        await tools.ChatEngineSuggestFixesWithTargetAsync("sample.cs", "hello", CancellationToken.None);

        Assert.Equal(2, pipeClient.ProposeTextEditCalls);
        Assert.Equal(0, pipeClient.ProposeTextEditsCalls);
    }

    private static void AssertChatEngineChatResultJson(string responseJson, string expectedContent)
    {
        var response = JsonSerializer.Deserialize<ChatEngineChatResult>(responseJson);

        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.Equal(expectedContent, response.Content);
        Assert.Null(response.Error);
        Assert.Null(response.ErrorCode);
        Assert.False(string.IsNullOrWhiteSpace(response.RequestId));
    }

    private static void AssertFailureErrorCode(string responseJson, string expectedErrorCode)
    {
        var response = JsonSerializer.Deserialize<ChatEngineChatResult>(responseJson);

        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Null(response.Content);
        Assert.Equal(expectedErrorCode, response.ErrorCode);
        Assert.False(string.IsNullOrWhiteSpace(response.RequestId));
    }

    private sealed class RecordingPipeClient : IPipeClient
    {
        public int ProposeTextEditCalls { get; private set; }
        public int ProposeTextEditsCalls { get; private set; }
        public string? LastProposedFilePath { get; private set; }
        public string? LastProposedOriginalText { get; private set; }
        public string? LastProposedText { get; private set; }
        public IReadOnlyList<ProposalFileEditRequest> LastFileEdits { get; private set; } = Array.Empty<ProposalFileEditRequest>();

        public Task<GetActiveDocumentResponse> GetActiveDocumentAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task<GetErrorListResponse> GetErrorListAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task<GetSelectedTextResponse> GetSelectedTextAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task<ListSolutionProjectsResponse> ListSolutionProjectsAsync(CancellationToken ct = default) => throw new NotSupportedException();

        public Task<ProposeTextEditResponse> ProposeTextEditAsync(string filePath, string originalText, string proposedText, CancellationToken ct = default)
        {
            ProposeTextEditCalls++;
            LastProposedFilePath = filePath;
            LastProposedOriginalText = originalText;
            LastProposedText = proposedText;
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

    private sealed class StubChatEngine : IChatEngine
    {
        private readonly string? _fixedResponse;

        public StubChatEngine(string? fixedResponse = null)
        {
            _fixedResponse = fixedResponse;
        }

        public ChatRequest? LastRequest { get; private set; }

        public Task<ChatResponse> SendAsync(ChatRequest request, CancellationToken cancellationToken, Action<ChatEvent>? onEvent = null)
        {
            LastRequest = request;
            var response = _fixedResponse ?? (request.Message == "ping" ? "pong" : $"echo:{request.Message}");
            return Task.FromResult(new ChatResponse(response));
        }

        public async IAsyncEnumerable<ChatEvent> StreamAsync(ChatRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            LastRequest = request;
            yield return new ChatEvent(ChatEventType.RequestStarted);
            var response = _fixedResponse ?? (request.Message == "ping" ? "pong" : $"echo:{request.Message}");
            yield return new ChatEvent(ChatEventType.TokenGenerated, response);
            yield return new ChatEvent(ChatEventType.ResponseCompleted);
            await Task.CompletedTask;
        }
    }

    private sealed class ThrowingChatEngine : IChatEngine
    {
        public ChatRequest? LastRequest { get; private set; }

        public Task<ChatResponse> SendAsync(ChatRequest request, CancellationToken cancellationToken, Action<ChatEvent>? onEvent = null)
        {
            LastRequest = request;
            throw new InvalidOperationException("provider failed");
        }

        public async IAsyncEnumerable<ChatEvent> StreamAsync(ChatRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            LastRequest = request;
            await Task.CompletedTask;
            yield break;
            throw new InvalidOperationException("provider failed");
        }
    }

    private sealed class TrackingChatEngine : IChatEngine
    {
        private readonly string _response;

        public TrackingChatEngine(string response)
        {
            _response = response;
        }

        public int SendAsyncCalls { get; private set; }
        public int StreamAsyncCalls { get; private set; }

        public Task<ChatResponse> SendAsync(ChatRequest request, CancellationToken cancellationToken, Action<ChatEvent>? onEvent = null)
        {
            SendAsyncCalls++;
            return Task.FromResult(new ChatResponse(_response));
        }

        public async IAsyncEnumerable<ChatEvent> StreamAsync(ChatRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            StreamAsyncCalls++;
            await Task.CompletedTask;
            yield break;
        }
    }

    private sealed class InventoryMcpProbeBridgeTool : IBridgeTool
    {
        public InventoryMcpProbeBridgeTool(
            string id,
            string category,
            BridgeToolDiscoveryKind discoveryKind,
            string source,
            string host,
            IReadOnlyList<BridgeCapability> requiredCapabilities,
            ToolExecutionApprovalRequirement approvalRequirement)
        {
            Descriptor = new BridgeToolDescriptor
            {
                Id = id,
                Name = $"Fake {id}",
                Description = $"Fake inventory tool {id}.",
                Category = category,
                Source = source,
                Host = host,
                DiscoveryKind = discoveryKind,
                RequiredCapabilities = requiredCapabilities,
                ApprovalRequirement = approvalRequirement
            };
        }

        public BridgeToolDescriptor Descriptor { get; }

        public int ExecutionCount { get; private set; }

        public Task<BridgeToolResult> ExecuteAsync(BridgeToolRequest request, CancellationToken cancellationToken)
        {
            ExecutionCount++;
            return Task.FromResult(BridgeToolResult.Succeeded(request, "executed"));
        }
    }

}
