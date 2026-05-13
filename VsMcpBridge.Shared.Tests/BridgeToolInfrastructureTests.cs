using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VsMcpBridge.Shared.Composition;
using VsMcpBridge.Shared.Loggers;
using VsMcpBridge.Shared.Tools;
using Xunit;

namespace VsMcpBridge.Shared.Tests;

public sealed class BridgeToolInfrastructureTests
{
    [Fact]
    public void AddBridgeToolServices_registers_catalog_and_executor()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILogger, RecordingBridgeLogger>();

        services.AddBridgeToolServices();
        using var provider = services.BuildServiceProvider();

        Assert.IsType<CompiledBridgeToolCatalog>(provider.GetRequiredService<IBridgeToolCatalog>());
        Assert.IsType<BridgeToolExecutor>(provider.GetRequiredService<IBridgeToolExecutor>());
    }

    [Fact]
    public void AddMvpVmServices_exposes_bridge_tool_catalog_and_executor()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILogger, RecordingBridgeLogger>();

        services.AddMvpVmServices();
        using var provider = services.BuildServiceProvider();

        Assert.IsType<CompiledBridgeToolCatalog>(provider.GetRequiredService<IBridgeToolCatalog>());
        Assert.IsType<BridgeToolExecutor>(provider.GetRequiredService<IBridgeToolExecutor>());
    }

    [Fact]
    public void Empty_compiled_catalog_returns_no_tools_cleanly()
    {
        var catalog = new CompiledBridgeToolCatalog(Enumerable.Empty<IBridgeTool>());

        var descriptors = catalog.GetTools();

        Assert.Empty(descriptors);
        Assert.False(catalog.TryGetTool("missing.tool", out _));
    }

    [Fact]
    public async Task Executor_returns_clear_failure_for_unknown_tool_id()
    {
        var logger = new RecordingBridgeLogger();
        var executor = new BridgeToolExecutor(new CompiledBridgeToolCatalog(Enumerable.Empty<IBridgeTool>()), logger);
        var request = CreateRequest("missing.tool");

        var result = await executor.ExecuteAsync(request, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("UnknownTool", result.ErrorCode);
        Assert.Equal("missing.tool", result.ToolId);
        Assert.Equal("request-123", result.RequestId);
        Assert.Equal("operation-456", result.OperationId);
        Assert.Contains("Unknown bridge tool 'missing.tool'.", result.Message);
        Assert.Contains(logger.InformationMessages, message => message.Contains("Bridge tool execution started")
            && message.Contains("[ToolId=missing.tool]")
            && message.Contains("[RequestId=request-123]")
            && message.Contains("[OperationId=operation-456]"));
        Assert.Contains(logger.WarningMessages, message => message.Contains("Bridge tool execution failed")
            && message.Contains("[ErrorCode=UnknownTool]")
            && message.Contains("[RequestId=request-123]")
            && message.Contains("[OperationId=operation-456]"));
    }

    [Fact]
    public async Task Executor_invokes_registered_tool_and_preserves_correlation_metadata()
    {
        var logger = new RecordingBridgeLogger();
        var tool = new FakeBridgeTool();
        var executor = new BridgeToolExecutor(new CompiledBridgeToolCatalog(new[] { tool }), logger);
        var request = CreateRequest("fake.echo");

        var result = await executor.ExecuteAsync(request, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("fake.echo", result.ToolId);
        Assert.Equal("request-123", result.RequestId);
        Assert.Equal("operation-456", result.OperationId);
        Assert.Equal("Echo completed.", result.Message);
        Assert.Same(request, tool.LastRequest);
        Assert.Contains(logger.InformationMessages, message => message.Contains("Bridge tool execution started")
            && message.Contains("[ToolId=fake.echo]")
            && message.Contains("[RequestId=request-123]")
            && message.Contains("[OperationId=operation-456]"));
        Assert.Contains(logger.InformationMessages, message => message.Contains("Bridge tool execution completed")
            && message.Contains("[ToolId=fake.echo]")
            && message.Contains("[Success=True]")
            && message.Contains("[RequestId=request-123]")
            && message.Contains("[OperationId=operation-456]"));
    }

    [Fact]
    public async Task Executor_returns_structured_failure_and_logs_boundary_when_tool_throws()
    {
        var logger = new RecordingBridgeLogger();
        var executor = new BridgeToolExecutor(
            new CompiledBridgeToolCatalog(new IBridgeTool[] { new ThrowingBridgeTool() }),
            logger);
        var request = CreateRequest("fake.throwing");

        var result = await executor.ExecuteAsync(request, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("ExecutionFailed", result.ErrorCode);
        Assert.Equal("fake.throwing", result.ToolId);
        Assert.Equal("request-123", result.RequestId);
        Assert.Equal("operation-456", result.OperationId);
        Assert.Contains(logger.Errors, error => error.Message.Contains("Bridge tool execution failed")
            && error.Message.Contains("[ToolId=fake.throwing]")
            && error.Message.Contains("[ErrorCode=ExecutionFailed]")
            && error.Message.Contains("[RequestId=request-123]")
            && error.Message.Contains("[OperationId=operation-456]")
            && error.Exception is System.InvalidOperationException);
    }

    private static BridgeToolRequest CreateRequest(string toolId)
        => new BridgeToolRequest
        {
            ToolId = toolId,
            RequestId = "request-123",
            OperationId = "operation-456",
            Arguments = new Dictionary<string, object?> { ["input"] = "hello" }
        };

    private sealed class FakeBridgeTool : IBridgeTool
    {
        public BridgeToolDescriptor Descriptor { get; } = new BridgeToolDescriptor
        {
            Id = "fake.echo",
            Name = "Fake Echo",
            Description = "Fake test tool.",
            Category = "Tests",
            Source = "Compiled",
            Host = "SharedTests"
        };

        public BridgeToolRequest? LastRequest { get; private set; }

        public Task<BridgeToolResult> ExecuteAsync(BridgeToolRequest request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(BridgeToolResult.Succeeded(
                request,
                "Echo completed.",
                new Dictionary<string, object?> { ["echo"] = request.Arguments["input"] }));
        }
    }

    private sealed class ThrowingBridgeTool : IBridgeTool
    {
        public BridgeToolDescriptor Descriptor { get; } = new BridgeToolDescriptor
        {
            Id = "fake.throwing",
            Name = "Fake Throwing",
            Description = "Fake failing test tool."
        };

        public Task<BridgeToolResult> ExecuteAsync(BridgeToolRequest request, CancellationToken cancellationToken)
            => throw new System.InvalidOperationException("fake failure");
    }
}
