using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VsMcpBridge.Shared.Composition;
using VsMcpBridge.Shared.Loggers;
using VsMcpBridge.Shared.Security;
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
        Assert.IsType<BridgeSecurityRedactor>(provider.GetRequiredService<ISecurityRedactor>());
        Assert.IsType<NoOpAuditSink>(provider.GetRequiredService<IAuditSink>());
        Assert.IsType<AllowToolExecutionPolicy>(provider.GetRequiredService<IToolExecutionPolicy>());
        Assert.Contains(provider.GetRequiredService<IBridgeToolCatalog>().GetTools(), tool => tool.Id == RegexTextSearchTool.ToolId);
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
        Assert.IsType<BridgeSecurityRedactor>(provider.GetRequiredService<ISecurityRedactor>());
        Assert.IsType<NoOpAuditSink>(provider.GetRequiredService<IAuditSink>());
        Assert.IsType<AllowToolExecutionPolicy>(provider.GetRequiredService<IToolExecutionPolicy>());
        Assert.Contains(provider.GetRequiredService<IBridgeToolCatalog>().GetTools(), tool => tool.Id == RegexTextSearchTool.ToolId);
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
        var auditSink = new InMemoryAuditSink();
        var tool = new FakeBridgeTool();
        var executor = new BridgeToolExecutor(
            new CompiledBridgeToolCatalog(new[] { tool }),
            logger,
            new BridgeSecurityRedactor(),
            auditSink,
            new AllowToolExecutionPolicy());
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
        var auditEvent = Assert.Single(auditSink.Events);
        Assert.Equal("BridgeToolExecution", auditEvent.EventName);
        Assert.True(auditEvent.Allowed);
        Assert.True(auditEvent.Success);
        Assert.Equal("fake.echo", auditEvent.ToolId);
        Assert.Equal("request-123", auditEvent.RequestId);
        Assert.Equal("operation-456", auditEvent.OperationId);
    }

    [Fact]
    public async Task Executor_returns_structured_failure_and_logs_boundary_when_tool_throws()
    {
        var logger = new RecordingBridgeLogger();
        var auditSink = new InMemoryAuditSink();
        var executor = new BridgeToolExecutor(
            new CompiledBridgeToolCatalog(new IBridgeTool[] { new ThrowingBridgeTool() }),
            logger,
            new BridgeSecurityRedactor(),
            auditSink,
            new AllowToolExecutionPolicy());
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
        var auditEvent = Assert.Single(auditSink.Events);
        Assert.True(auditEvent.Allowed);
        Assert.False(auditEvent.Success);
        Assert.Equal("ExecutionFailed", auditEvent.ErrorCode);
        Assert.Equal("request-123", auditEvent.RequestId);
        Assert.Equal("operation-456", auditEvent.OperationId);
    }

    [Fact]
    public async Task Executor_default_policy_allows_compiled_regex_search_tool()
    {
        var logger = new RecordingBridgeLogger();
        var executor = new BridgeToolExecutor(
            new CompiledBridgeToolCatalog(new IBridgeTool[] { new RegexTextSearchTool() }),
            logger,
            new BridgeSecurityRedactor(),
            new InMemoryAuditSink(),
            new AllowToolExecutionPolicy());

        var result = await executor.ExecuteAsync(CreateRegexRequest("error", new[] { "one error" }), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(RegexTextSearchTool.ToolId, result.ToolId);
        Assert.Equal("request-123", result.RequestId);
        Assert.Equal("operation-456", result.OperationId);
    }

    [Fact]
    public async Task Executor_deny_policy_prevents_tool_execution_and_emits_audit_event()
    {
        var logger = new RecordingBridgeLogger();
        var auditSink = new InMemoryAuditSink();
        var tool = new FakeBridgeTool();
        var executor = new BridgeToolExecutor(
            new CompiledBridgeToolCatalog(new[] { tool }),
            logger,
            new BridgeSecurityRedactor(),
            auditSink,
            new DenyToolExecutionPolicy("test token=raw-deny-secret"));
        var request = CreateRequest("fake.echo");

        var result = await executor.ExecuteAsync(request, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("PolicyDenied", result.ErrorCode);
        Assert.Equal("fake.echo", result.ToolId);
        Assert.Equal("request-123", result.RequestId);
        Assert.Equal("operation-456", result.OperationId);
        Assert.Null(tool.LastRequest);
        Assert.DoesNotContain(logger.WarningMessages, message => message.Contains("raw-deny-secret"));
        var auditEvent = Assert.Single(auditSink.Events);
        Assert.False(auditEvent.Allowed);
        Assert.False(auditEvent.Success);
        Assert.Equal("PolicyDenied", auditEvent.ErrorCode);
        Assert.Equal("fake.echo", auditEvent.ToolId);
        Assert.Equal("request-123", auditEvent.RequestId);
        Assert.Equal("operation-456", auditEvent.OperationId);
        Assert.Equal("test token=[REDACTED]", auditEvent.Metadata["policyReason"]);
    }

    [Fact]
    public void Security_redactor_masks_obvious_secret_values()
    {
        var redactor = new BridgeSecurityRedactor();

        var result = redactor.Redact(
            "apiKey=alpha token: beta password=\"gamma\" secret='delta' Authorization: Bearer epsilon");

        Assert.True(result.WasRedacted);
        Assert.DoesNotContain("alpha", result.Value);
        Assert.DoesNotContain("beta", result.Value);
        Assert.DoesNotContain("gamma", result.Value);
        Assert.DoesNotContain("delta", result.Value);
        Assert.DoesNotContain("epsilon", result.Value);
        Assert.Contains("[REDACTED]", result.Value);
    }

    [Fact]
    public async Task Executor_redacts_secret_like_values_from_tested_log_paths()
    {
        var logger = new RecordingBridgeLogger();
        var executor = new BridgeToolExecutor(
            new CompiledBridgeToolCatalog(new IBridgeTool[] { new SecretReturningBridgeTool(), new SecretThrowingBridgeTool() }),
            logger,
            new BridgeSecurityRedactor(),
            new InMemoryAuditSink(),
            new AllowToolExecutionPolicy());
        var request = new BridgeToolRequest
        {
            ToolId = "fake.secret",
            RequestId = "request-123",
            OperationId = "operation-456",
            Arguments = new Dictionary<string, object?> { ["apiKey"] = "raw-request-secret" }
        };

        var result = await executor.ExecuteAsync(request, CancellationToken.None);
        var failed = await executor.ExecuteAsync(
            new BridgeToolRequest
            {
                ToolId = "fake.secret.throw",
                RequestId = "request-123",
                OperationId = "operation-456",
                Arguments = new Dictionary<string, object?> { ["authorization"] = "Bearer raw-bearer-secret" }
            },
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.False(failed.Success);
        var logMessages = logger.VerboseMessages
            .Concat(logger.InformationMessages)
            .Concat(logger.WarningMessages)
            .Concat(logger.Errors.Select(error => error.Message))
            .Concat(logger.Errors.Select(error => error.Exception?.Message ?? string.Empty))
            .ToList();
        Assert.DoesNotContain(logMessages, message => message.Contains("raw-request-secret"));
        Assert.DoesNotContain(logMessages, message => message.Contains("raw-result-secret"));
        Assert.DoesNotContain(logMessages, message => message.Contains("raw-bearer-secret"));
        Assert.DoesNotContain(logMessages, message => message.Contains("raw-exception-secret"));
        Assert.Contains(logMessages, message => message.Contains("[REDACTED]"));
    }

    [Fact]
    public async Task Executor_invokes_compiled_regex_search_tool_by_tool_id()
    {
        var logger = new RecordingBridgeLogger();
        var services = new ServiceCollection();
        services.AddSingleton<ILogger>(logger);
        services.AddBridgeToolServices();
        using var provider = services.BuildServiceProvider();
        var executor = provider.GetRequiredService<IBridgeToolExecutor>();
        var request = CreateRegexRequest("error", new[] { "one error", "two warnings" });

        var result = await executor.ExecuteAsync(request, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(RegexTextSearchTool.ToolId, result.ToolId);
        Assert.Equal("request-123", result.RequestId);
        Assert.Equal("operation-456", result.OperationId);
        var matches = GetMatches(result);
        var match = Assert.Single(matches);
        Assert.Equal(0, match.EntryIndex);
        Assert.Equal("error", match.Value);
        Assert.Contains(logger.InformationMessages, message => message.Contains("Bridge tool execution started")
            && message.Contains($"[ToolId={RegexTextSearchTool.ToolId}]")
            && message.Contains("[RequestId=request-123]")
            && message.Contains("[OperationId=operation-456]"));
        Assert.Contains(logger.InformationMessages, message => message.Contains("Bridge tool execution completed")
            && message.Contains($"[ToolId={RegexTextSearchTool.ToolId}]")
            && message.Contains("[Success=True]")
            && message.Contains("[RequestId=request-123]")
            && message.Contains("[OperationId=operation-456]"));
    }

    [Fact]
    public async Task Regex_search_supports_literal_and_regex_patterns()
    {
        var result = await ExecuteRegexSearchAsync(@"\bCS\d{4}\b", new[] { "CS1002 expected ;", "no diagnostic", "CS1525 invalid term" });

        Assert.True(result.Success);
        var matches = GetMatches(result);
        Assert.Equal(2, matches.Count);
        Assert.Equal("CS1002", matches[0].Value);
        Assert.Equal("CS1525", matches[1].Value);
        Assert.Equal(2, result.Data["matchCount"]);
        Assert.Equal(2, result.Data["totalMatchCount"]);
    }

    [Fact]
    public async Task Regex_search_honors_case_sensitivity()
    {
        var insensitive = await ExecuteRegexSearchAsync("error", new[] { "Error", "error" }, caseSensitive: false);
        var sensitive = await ExecuteRegexSearchAsync("error", new[] { "Error", "error" }, caseSensitive: true);

        Assert.Equal(2, GetMatches(insensitive).Count);
        var sensitiveMatch = Assert.Single(GetMatches(sensitive));
        Assert.Equal("error", sensitiveMatch.Value);
    }

    [Fact]
    public async Task Regex_search_limits_returned_results()
    {
        var result = await ExecuteRegexSearchAsync("hit", new[] { "hit hit", "hit" }, maxResults: 2);

        Assert.True(result.Success);
        var matches = GetMatches(result);
        Assert.Equal(2, matches.Count);
        Assert.Equal(2, result.Data["matchCount"]);
        Assert.Equal(3, result.Data["totalMatchCount"]);
        Assert.Equal(true, result.Data["limited"]);
    }

    [Fact]
    public async Task Regex_search_returns_structured_failure_for_invalid_regex()
    {
        var result = await ExecuteRegexSearchAsync("[", new[] { "anything" });

        Assert.False(result.Success);
        Assert.Equal("InvalidRegex", result.ErrorCode);
        Assert.Equal(RegexTextSearchTool.ToolId, result.ToolId);
        Assert.Equal("request-123", result.RequestId);
        Assert.Equal("operation-456", result.OperationId);
        Assert.Contains("Invalid regular expression", result.Message);
    }

    private static BridgeToolRequest CreateRequest(string toolId)
        => new BridgeToolRequest
        {
            ToolId = toolId,
            RequestId = "request-123",
            OperationId = "operation-456",
            Arguments = new Dictionary<string, object?> { ["input"] = "hello" }
        };

    private static BridgeToolRequest CreateRegexRequest(string pattern, IReadOnlyList<string> entries, bool caseSensitive = false, int? maxResults = null)
    {
        var arguments = new Dictionary<string, object?>
        {
            ["pattern"] = pattern,
            ["entries"] = entries,
            ["caseSensitive"] = caseSensitive
        };

        if (maxResults.HasValue)
            arguments["maxResults"] = maxResults.Value;

        return new BridgeToolRequest
        {
            ToolId = RegexTextSearchTool.ToolId,
            RequestId = "request-123",
            OperationId = "operation-456",
            Arguments = arguments
        };
    }

    private static async Task<BridgeToolResult> ExecuteRegexSearchAsync(string pattern, IReadOnlyList<string> entries, bool caseSensitive = false, int? maxResults = null)
    {
        var logger = new RecordingBridgeLogger();
        var executor = new BridgeToolExecutor(
            new CompiledBridgeToolCatalog(new IBridgeTool[] { new RegexTextSearchTool() }),
            logger);

        return await executor.ExecuteAsync(CreateRegexRequest(pattern, entries, caseSensitive, maxResults), CancellationToken.None);
    }

    private static IReadOnlyList<RegexTextSearchMatch> GetMatches(BridgeToolResult result)
        => Assert.IsAssignableFrom<IReadOnlyList<RegexTextSearchMatch>>(result.Data["matches"]);

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

    private sealed class SecretReturningBridgeTool : IBridgeTool
    {
        public BridgeToolDescriptor Descriptor { get; } = new BridgeToolDescriptor
        {
            Id = "fake.secret",
            Name = "Fake Secret",
            Description = "Fake secret-returning test tool."
        };

        public Task<BridgeToolResult> ExecuteAsync(BridgeToolRequest request, CancellationToken cancellationToken)
            => Task.FromResult(BridgeToolResult.Succeeded(
                request,
                "Secret completed.",
                new Dictionary<string, object?> { ["token"] = "raw-result-secret" }));
    }

    private sealed class SecretThrowingBridgeTool : IBridgeTool
    {
        public BridgeToolDescriptor Descriptor { get; } = new BridgeToolDescriptor
        {
            Id = "fake.secret.throw",
            Name = "Fake Secret Throwing",
            Description = "Fake secret-throwing test tool."
        };

        public Task<BridgeToolResult> ExecuteAsync(BridgeToolRequest request, CancellationToken cancellationToken)
            => throw new System.InvalidOperationException("secret=raw-exception-secret");
    }

    private sealed class DenyToolExecutionPolicy : IToolExecutionPolicy
    {
        private readonly string _reason;

        public DenyToolExecutionPolicy(string reason)
        {
            _reason = reason;
        }

        public Task<ToolExecutionPolicyDecision> EvaluateAsync(ToolExecutionSecurityContext context, CancellationToken cancellationToken)
            => Task.FromResult(ToolExecutionPolicyDecision.Deny(_reason));
    }
}
