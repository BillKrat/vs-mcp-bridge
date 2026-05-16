using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;
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
        Assert.IsType<AllowToolExecutionApprovalService>(provider.GetRequiredService<IToolExecutionApprovalService>());
        Assert.Contains(provider.GetRequiredService<IBridgeToolCatalog>().GetTools(), tool => tool.Id == RegexTextSearchTool.ToolId);
        Assert.Contains(provider.GetRequiredService<IBridgeToolCatalog>().GetTools(), tool => tool.Id == Bm25TextSearchTool.ToolId);
        Assert.All(provider.GetRequiredService<IBridgeToolCatalog>().GetTools(), tool => Assert.Empty(tool.RequiredCapabilities));
        Assert.Contains(provider.GetServices<IBridgeToolDiscovery>(), discovery => discovery is CompiledBridgeToolDiscovery);
        Assert.Contains(provider.GetServices<IBridgeToolDiscovery>(), discovery => discovery is MefBridgeToolDiscovery);
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
        Assert.IsType<AllowToolExecutionApprovalService>(provider.GetRequiredService<IToolExecutionApprovalService>());
        Assert.Contains(provider.GetRequiredService<IBridgeToolCatalog>().GetTools(), tool => tool.Id == RegexTextSearchTool.ToolId);
        Assert.Contains(provider.GetRequiredService<IBridgeToolCatalog>().GetTools(), tool => tool.Id == Bm25TextSearchTool.ToolId);
        Assert.All(provider.GetRequiredService<IBridgeToolCatalog>().GetTools(), tool => Assert.Empty(tool.RequiredCapabilities));
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
    public void Compiled_catalog_returns_clear_failure_for_duplicate_tool_ids()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => new CompiledBridgeToolCatalog(new IBridgeTool[]
        {
            new FakeBridgeTool(),
            new DuplicateFakeBridgeTool()
        }));

        Assert.Contains("Duplicate bridge tool id 'fake.echo' discovered.", ex.Message);
        Assert.Contains(nameof(FakeBridgeTool), ex.Message);
        Assert.Contains(nameof(DuplicateFakeBridgeTool), ex.Message);
    }

    [Fact]
    public void Missing_mef_directory_does_not_fail_discovery_or_startup()
    {
        var logger = new RecordingBridgeLogger();
        var missingDirectory = Path.Combine(Path.GetTempPath(), "VsMcpBridgeMissingMefTools", Guid.NewGuid().ToString("N"));
        var services = new ServiceCollection();
        services.AddSingleton<ILogger>(logger);
        services.AddBridgeToolServices(options =>
        {
            options.EnableMefDirectoryDiscovery = true;
            options.MefDirectories.Add(missingDirectory);
        });
        using var provider = services.BuildServiceProvider();

        var catalog = provider.GetRequiredService<IBridgeToolCatalog>();

        Assert.Contains(catalog.GetTools(), tool => tool.Id == RegexTextSearchTool.ToolId);
        Assert.Contains(catalog.GetTools(), tool => tool.Id == Bm25TextSearchTool.ToolId);
        Assert.Contains(logger.InformationMessages, message => message.Contains("MEF bridge tool discovery started")
            && message.Contains("[Enabled=True]"));
        Assert.Contains(logger.WarningMessages, message => message.Contains("MEF bridge tool discovery directory missing")
            && message.Contains(missingDirectory));
        Assert.Contains(logger.InformationMessages, message => message.Contains("MEF bridge tool discovery completed")
            && message.Contains("[ToolCount=0]"));
    }

    [Fact]
    public void Mef_discovery_can_discover_exported_bridge_tool_when_enabled()
    {
        MefFakeBridgeTool.ExecutionCount = 0;
        var logger = new RecordingBridgeLogger();
        var services = new ServiceCollection();
        services.AddSingleton<ILogger>(logger);
        services.AddBridgeToolServices(options =>
        {
            options.EnableMefDirectoryDiscovery = true;
            options.MefDirectories.Add(AppContext.BaseDirectory);
            options.MefSearchPattern = "VsMcpBridge.Shared.Tests.dll";
        });
        using var provider = services.BuildServiceProvider();

        var catalog = provider.GetRequiredService<IBridgeToolCatalog>();

        Assert.Contains(catalog.GetTools(), tool => tool.Id == MefFakeBridgeTool.ToolId);
        Assert.True(catalog.TryGetTool(MefFakeBridgeTool.ToolId, out _));
        Assert.Equal(0, MefFakeBridgeTool.ExecutionCount);
        Assert.Contains(logger.InformationMessages, message => message.Contains("MEF bridge tool discovery completed")
            && message.Contains("[ToolCount=1]"));
    }

    [Fact]
    public void Mef_discovery_load_failures_are_logged_and_not_silent()
    {
        var logger = new RecordingBridgeLogger();
        var directory = Path.Combine(Path.GetTempPath(), "VsMcpBridgeBadMefTools", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllText(Path.Combine(directory, "not-an-assembly.dll"), "not a managed assembly");
            var options = new BridgeToolDiscoveryOptions
            {
                EnableMefDirectoryDiscovery = true
            };
            options.MefDirectories.Add(directory);

            var tools = new MefBridgeToolDiscovery(options, logger).DiscoverTools();

            Assert.Empty(tools);
            Assert.Contains(logger.WarningMessages, message => message.Contains("MEF bridge tool discovery failed to load assembly")
                && message.Contains("not-an-assembly.dll"));
            Assert.Contains(logger.InformationMessages, message => message.Contains("MEF bridge tool discovery completed")
                && message.Contains("[ToolCount=0]"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Mef_discovered_tool_still_runs_through_executor_security_and_audit_boundary()
    {
        MefFakeBridgeTool.ExecutionCount = 0;
        var logger = new RecordingBridgeLogger();
        var auditSink = new InMemoryAuditSink();
        var services = new ServiceCollection();
        services.AddSingleton<ILogger>(logger);
        services.AddSingleton<IAuditSink>(auditSink);
        services.AddSingleton<IToolExecutionPolicy>(new DenyToolExecutionPolicy("blocked token=mef-secret"));
        services.AddBridgeToolServices(options =>
        {
            options.EnableMefDirectoryDiscovery = true;
            options.MefDirectories.Add(AppContext.BaseDirectory);
            options.MefSearchPattern = "VsMcpBridge.Shared.Tests.dll";
        });
        using var provider = services.BuildServiceProvider();
        var executor = provider.GetRequiredService<IBridgeToolExecutor>();

        var result = await executor.ExecuteAsync(CreateRequest(MefFakeBridgeTool.ToolId), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("PolicyDenied", result.ErrorCode);
        Assert.Equal(0, MefFakeBridgeTool.ExecutionCount);
        var auditEvent = Assert.Single(auditSink.Events);
        Assert.False(auditEvent.Allowed);
        Assert.Equal(MefFakeBridgeTool.ToolId, auditEvent.ToolId);
        Assert.Equal("blocked token=[REDACTED]", auditEvent.Metadata["policyReason"]);
        Assert.DoesNotContain(logger.WarningMessages, message => message.Contains("mef-secret"));
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
        Assert.Equal("None", auditEvent.Metadata["requiredCapabilities"]);
    }

    [Fact]
    public async Task Executor_flows_required_capabilities_to_policy_and_audit_metadata()
    {
        var logger = new RecordingBridgeLogger();
        var auditSink = new InMemoryAuditSink();
        var policy = new RecordingCapabilityPolicy(ToolExecutionPolicyDecision.Allow("capability policy allowed"));
        var tool = new CapabilityBridgeTool(new[]
        {
            new BridgeCapability("workspace.read"),
            new BridgeCapability("token=capability-secret")
        });
        var executor = new BridgeToolExecutor(
            new CompiledBridgeToolCatalog(new[] { tool }),
            logger,
            new BridgeSecurityRedactor(),
            auditSink,
            policy);
        var request = CreateRequest(CapabilityBridgeTool.ToolId);

        var result = await executor.ExecuteAsync(request, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Same(request, tool.LastRequest);
        Assert.Equal(1, policy.CallCount);
        Assert.NotNull(policy.LastContext);
        Assert.Equal(CapabilityBridgeTool.ToolId, policy.LastContext!.ToolId);
        Assert.Equal("request-123", policy.LastContext.RequestId);
        Assert.Equal("operation-456", policy.LastContext.OperationId);
        Assert.Equal(new[] { "workspace.read", "token=capability-secret" }, policy.LastContext.RequiredCapabilities.Select(capability => capability.Name).ToArray());
        Assert.Contains(logger.VerboseMessages, message => message.Contains("Bridge tool capability metadata")
            && message.Contains("[RequiredCapabilities=workspace.read,token=[REDACTED]]"));
        Assert.DoesNotContain(logger.VerboseMessages, message => message.Contains("capability-secret"));
        var auditEvent = Assert.Single(auditSink.Events);
        Assert.True(auditEvent.Allowed);
        Assert.True(auditEvent.Success);
        Assert.Equal(CapabilityBridgeTool.ToolId, auditEvent.ToolId);
        Assert.Equal("request-123", auditEvent.RequestId);
        Assert.Equal("operation-456", auditEvent.OperationId);
        Assert.Equal("capability policy allowed", auditEvent.Metadata["policyReason"]);
        Assert.Equal("workspace.read,token=[REDACTED]", auditEvent.Metadata["requiredCapabilities"]);
    }

    [Fact]
    public async Task Executor_preserves_capability_metadata_when_policy_denies()
    {
        var logger = new RecordingBridgeLogger();
        var auditSink = new InMemoryAuditSink();
        var policy = new RecordingCapabilityPolicy(ToolExecutionPolicyDecision.Deny("missing capability"));
        var tool = new CapabilityBridgeTool(new[] { new BridgeCapability("workspace.write") });
        var executor = new BridgeToolExecutor(
            new CompiledBridgeToolCatalog(new[] { tool }),
            logger,
            new BridgeSecurityRedactor(),
            auditSink,
            policy);
        var request = CreateRequest(CapabilityBridgeTool.ToolId);

        var result = await executor.ExecuteAsync(request, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("PolicyDenied", result.ErrorCode);
        Assert.Null(tool.LastRequest);
        Assert.Equal(new[] { "workspace.write" }, policy.LastContext!.RequiredCapabilities.Select(capability => capability.Name).ToArray());
        var auditEvent = Assert.Single(auditSink.Events);
        Assert.False(auditEvent.Allowed);
        Assert.False(auditEvent.Success);
        Assert.Equal("PolicyDenied", auditEvent.ErrorCode);
        Assert.Equal("workspace.write", auditEvent.Metadata["requiredCapabilities"]);
        Assert.Equal("request-123", auditEvent.RequestId);
        Assert.Equal("operation-456", auditEvent.OperationId);
    }

    [Fact]
    public async Task Executor_skips_approval_service_for_tools_without_approval_requirement()
    {
        var logger = new RecordingBridgeLogger();
        var auditSink = new InMemoryAuditSink();
        var approvalService = new RecordingToolExecutionApprovalService(ToolExecutionApprovalDecision.Deny("should not be called"));
        var tool = new FakeBridgeTool();
        var executor = new BridgeToolExecutor(
            new CompiledBridgeToolCatalog(new[] { tool }),
            logger,
            new BridgeSecurityRedactor(),
            auditSink,
            new AllowToolExecutionPolicy(),
            approvalService);
        var request = CreateRequest("fake.echo");

        var result = await executor.ExecuteAsync(request, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Same(request, tool.LastRequest);
        Assert.Equal(0, approvalService.CallCount);
        var auditEvent = Assert.Single(auditSink.Events);
        Assert.True(auditEvent.Allowed);
        Assert.True(auditEvent.Success);
        Assert.Equal("NotRequired", auditEvent.Metadata["approvalRequirement"]);
        Assert.Equal("NotRequired", auditEvent.Metadata["approvalDecision"]);
        Assert.Equal("Not required", auditEvent.Metadata["approvalReason"]);
        Assert.Equal("request-123", auditEvent.RequestId);
        Assert.Equal("operation-456", auditEvent.OperationId);
    }

    [Fact]
    public async Task Executor_invokes_approval_service_for_approval_required_tool()
    {
        var logger = new RecordingBridgeLogger();
        var auditSink = new InMemoryAuditSink();
        var approvalService = new RecordingToolExecutionApprovalService(ToolExecutionApprovalDecision.Approve("operator approved token=approval-secret"));
        var tool = new ApprovalRequiredBridgeTool();
        var executor = new BridgeToolExecutor(
            new CompiledBridgeToolCatalog(new[] { tool }),
            logger,
            new BridgeSecurityRedactor(),
            auditSink,
            new AllowToolExecutionPolicy(),
            approvalService);
        var request = CreateRequest(ApprovalRequiredBridgeTool.ToolId);

        var result = await executor.ExecuteAsync(request, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Same(request, tool.LastRequest);
        Assert.Equal(1, approvalService.CallCount);
        Assert.NotNull(approvalService.LastContext);
        Assert.Equal(ApprovalRequiredBridgeTool.ToolId, approvalService.LastContext!.ToolId);
        Assert.Equal("request-123", approvalService.LastContext.RequestId);
        Assert.Equal("operation-456", approvalService.LastContext.OperationId);
        Assert.True(approvalService.LastContext.PolicyDecision.Allowed);
        Assert.Contains(logger.InformationMessages, message => message.Contains("Bridge tool execution approval required")
            && message.Contains($"[ToolId={ApprovalRequiredBridgeTool.ToolId}]")
            && message.Contains("[RequestId=request-123]")
            && message.Contains("[OperationId=operation-456]"));
        var auditEvent = Assert.Single(auditSink.Events);
        Assert.True(auditEvent.Allowed);
        Assert.True(auditEvent.Success);
        Assert.Equal(ApprovalRequiredBridgeTool.ToolId, auditEvent.ToolId);
        Assert.Equal("request-123", auditEvent.RequestId);
        Assert.Equal("operation-456", auditEvent.OperationId);
        Assert.Equal("Required", auditEvent.Metadata["approvalRequirement"]);
        Assert.Equal("Approved", auditEvent.Metadata["approvalDecision"]);
        Assert.Equal("operator approved token=[REDACTED]", auditEvent.Metadata["approvalReason"]);
    }

    [Fact]
    public async Task Executor_denied_approval_prevents_tool_execution_and_emits_audit_event()
    {
        var logger = new RecordingBridgeLogger();
        var auditSink = new InMemoryAuditSink();
        var approvalService = new RecordingToolExecutionApprovalService(ToolExecutionApprovalDecision.Deny("operator denied token=approval-secret"));
        var tool = new ApprovalRequiredBridgeTool();
        var executor = new BridgeToolExecutor(
            new CompiledBridgeToolCatalog(new[] { tool }),
            logger,
            new BridgeSecurityRedactor(),
            auditSink,
            new AllowToolExecutionPolicy(),
            approvalService);
        var request = CreateRequest(ApprovalRequiredBridgeTool.ToolId);

        var result = await executor.ExecuteAsync(request, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("ApprovalDenied", result.ErrorCode);
        Assert.Equal(ApprovalRequiredBridgeTool.ToolId, result.ToolId);
        Assert.Equal("request-123", result.RequestId);
        Assert.Equal("operation-456", result.OperationId);
        Assert.Null(tool.LastRequest);
        Assert.Equal(1, approvalService.CallCount);
        Assert.DoesNotContain(logger.WarningMessages, message => message.Contains("approval-secret"));
        var auditEvent = Assert.Single(auditSink.Events);
        Assert.False(auditEvent.Allowed);
        Assert.False(auditEvent.Success);
        Assert.Equal("ApprovalDenied", auditEvent.ErrorCode);
        Assert.Equal(ApprovalRequiredBridgeTool.ToolId, auditEvent.ToolId);
        Assert.Equal("request-123", auditEvent.RequestId);
        Assert.Equal("operation-456", auditEvent.OperationId);
        Assert.Equal("Allowed", auditEvent.Metadata["policyReason"]);
        Assert.Equal("Required", auditEvent.Metadata["approvalRequirement"]);
        Assert.Equal("Denied", auditEvent.Metadata["approvalDecision"]);
        Assert.Equal("operator denied token=[REDACTED]", auditEvent.Metadata["approvalReason"]);
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

    [Fact]
    public async Task Executor_invokes_compiled_bm25_search_tool_by_tool_id()
    {
        var logger = new RecordingBridgeLogger();
        var services = new ServiceCollection();
        services.AddSingleton<ILogger>(logger);
        services.AddBridgeToolServices();
        using var provider = services.BuildServiceProvider();
        var executor = provider.GetRequiredService<IBridgeToolExecutor>();
        var request = CreateBm25Request("build error", new[]
        {
            new Bm25TextSearchDocument { Id = "compiler", Text = "build error error compiler" },
            new Bm25TextSearchDocument { Id = "docs", Text = "documentation update" }
        });

        var result = await executor.ExecuteAsync(request, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(Bm25TextSearchTool.ToolId, result.ToolId);
        Assert.Equal("request-123", result.RequestId);
        Assert.Equal("operation-456", result.OperationId);
        var match = Assert.Single(GetBm25Results(result));
        Assert.Equal("compiler", match.DocumentId);
        Assert.Contains(logger.InformationMessages, message => message.Contains("Bridge tool execution started")
            && message.Contains($"[ToolId={Bm25TextSearchTool.ToolId}]")
            && message.Contains("[RequestId=request-123]")
            && message.Contains("[OperationId=operation-456]"));
        Assert.Contains(logger.InformationMessages, message => message.Contains("Bridge tool execution completed")
            && message.Contains($"[ToolId={Bm25TextSearchTool.ToolId}]")
            && message.Contains("[Success=True]")
            && message.Contains("[RequestId=request-123]")
            && message.Contains("[OperationId=operation-456]"));
    }

    [Fact]
    public async Task Bm25_search_ranks_more_relevant_documents_first()
    {
        var result = await ExecuteBm25SearchAsync("compile error", new[]
        {
            new Bm25TextSearchDocument { Id = "general", Text = "compile warning" },
            new Bm25TextSearchDocument { Id = "best", Text = "compile error error failure" },
            new Bm25TextSearchDocument { Id = "none", Text = "window layout" }
        });

        Assert.True(result.Success);
        var results = GetBm25Results(result);
        Assert.Equal(2, results.Count);
        Assert.Equal("best", results[0].DocumentId);
        Assert.Equal("general", results[1].DocumentId);
        Assert.True(results[0].Score > results[1].Score);
        Assert.Equal(2, result.Data["resultCount"]);
        Assert.Equal(2, result.Data["totalResultCount"]);
        Assert.Equal(false, result.Data["limited"]);
    }

    [Fact]
    public async Task Bm25_search_limits_returned_results()
    {
        var result = await ExecuteBm25SearchAsync("error", new[]
        {
            new Bm25TextSearchDocument { Id = "one", Text = "error error" },
            new Bm25TextSearchDocument { Id = "two", Text = "error" },
            new Bm25TextSearchDocument { Id = "three", Text = "another error" }
        }, maxResults: 2);

        Assert.True(result.Success);
        Assert.Equal(2, GetBm25Results(result).Count);
        Assert.Equal(2, result.Data["resultCount"]);
        Assert.Equal(3, result.Data["totalResultCount"]);
        Assert.Equal(true, result.Data["limited"]);
    }

    [Fact]
    public async Task Bm25_search_returns_structured_failure_for_empty_query()
    {
        var result = await ExecuteBm25SearchAsync(" ", new[]
        {
            new Bm25TextSearchDocument { Id = "one", Text = "searchable text" }
        });

        Assert.False(result.Success);
        Assert.Equal("InvalidRequest", result.ErrorCode);
        Assert.Equal(Bm25TextSearchTool.ToolId, result.ToolId);
        Assert.Equal("request-123", result.RequestId);
        Assert.Equal("operation-456", result.OperationId);
        Assert.Contains("non-empty 'query'", result.Message);
    }

    [Fact]
    public async Task Bm25_search_returns_structured_failure_for_empty_documents()
    {
        var result = await ExecuteBm25SearchAsync("query", Array.Empty<Bm25TextSearchDocument>());

        Assert.False(result.Success);
        Assert.Equal("InvalidRequest", result.ErrorCode);
        Assert.Equal(Bm25TextSearchTool.ToolId, result.ToolId);
        Assert.Equal("request-123", result.RequestId);
        Assert.Equal("operation-456", result.OperationId);
        Assert.Contains("non-empty 'documents' or 'entries'", result.Message);
    }

    [Fact]
    public async Task Bm25_search_preserves_correlation_metadata_through_executor()
    {
        var result = await ExecuteBm25SearchAsync("error", new[]
        {
            new Bm25TextSearchDocument { Id = "one", Text = "error" }
        });

        Assert.True(result.Success);
        Assert.Equal(Bm25TextSearchTool.ToolId, result.ToolId);
        Assert.Equal("request-123", result.RequestId);
        Assert.Equal("operation-456", result.OperationId);
    }

    [Fact]
    public async Task Bm25_search_uses_executor_policy_redaction_and_audit_boundary()
    {
        var logger = new RecordingBridgeLogger();
        var auditSink = new InMemoryAuditSink();
        var executor = new BridgeToolExecutor(
            new CompiledBridgeToolCatalog(new IBridgeTool[] { new Bm25TextSearchTool() }),
            logger,
            new BridgeSecurityRedactor(),
            auditSink,
            new DenyToolExecutionPolicy("blocked token=raw-bm25-secret"));
        var request = CreateBm25Request("secret", new[]
        {
            new Bm25TextSearchDocument { Id = "one", Text = "secret text" }
        });

        var result = await executor.ExecuteAsync(request, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("PolicyDenied", result.ErrorCode);
        Assert.Equal(Bm25TextSearchTool.ToolId, result.ToolId);
        Assert.Equal("request-123", result.RequestId);
        Assert.Equal("operation-456", result.OperationId);
        Assert.DoesNotContain(logger.WarningMessages, message => message.Contains("raw-bm25-secret"));
        var auditEvent = Assert.Single(auditSink.Events);
        Assert.False(auditEvent.Allowed);
        Assert.False(auditEvent.Success);
        Assert.Equal("PolicyDenied", auditEvent.ErrorCode);
        Assert.Equal(Bm25TextSearchTool.ToolId, auditEvent.ToolId);
        Assert.Equal("request-123", auditEvent.RequestId);
        Assert.Equal("operation-456", auditEvent.OperationId);
        Assert.Equal("blocked token=[REDACTED]", auditEvent.Metadata["policyReason"]);
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

    private static BridgeToolRequest CreateBm25Request(string query, IReadOnlyList<Bm25TextSearchDocument> documents, bool caseSensitive = false, int? maxResults = null)
    {
        var arguments = new Dictionary<string, object?>
        {
            ["query"] = query,
            ["documents"] = documents,
            ["caseSensitive"] = caseSensitive
        };

        if (maxResults.HasValue)
            arguments["maxResults"] = maxResults.Value;

        return new BridgeToolRequest
        {
            ToolId = Bm25TextSearchTool.ToolId,
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

    private static async Task<BridgeToolResult> ExecuteBm25SearchAsync(string query, IReadOnlyList<Bm25TextSearchDocument> documents, bool caseSensitive = false, int? maxResults = null)
    {
        var logger = new RecordingBridgeLogger();
        var executor = new BridgeToolExecutor(
            new CompiledBridgeToolCatalog(new IBridgeTool[] { new Bm25TextSearchTool() }),
            logger);

        return await executor.ExecuteAsync(CreateBm25Request(query, documents, caseSensitive, maxResults), CancellationToken.None);
    }

    private static IReadOnlyList<RegexTextSearchMatch> GetMatches(BridgeToolResult result)
        => Assert.IsAssignableFrom<IReadOnlyList<RegexTextSearchMatch>>(result.Data["matches"]);

    private static IReadOnlyList<Bm25TextSearchResult> GetBm25Results(BridgeToolResult result)
        => Assert.IsAssignableFrom<IReadOnlyList<Bm25TextSearchResult>>(result.Data["results"]);

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

    private sealed class CapabilityBridgeTool : IBridgeTool
    {
        public const string ToolId = "fake.capability";

        public CapabilityBridgeTool(IReadOnlyList<BridgeCapability> capabilities)
        {
            Descriptor = new BridgeToolDescriptor
            {
                Id = ToolId,
                Name = "Fake Capability",
                Description = "Fake capability metadata test tool.",
                Category = "Tests",
                Source = "Compiled",
                Host = "SharedTests",
                RequiredCapabilities = capabilities
            };
        }

        public BridgeToolDescriptor Descriptor { get; }

        public BridgeToolRequest? LastRequest { get; private set; }

        public Task<BridgeToolResult> ExecuteAsync(BridgeToolRequest request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(BridgeToolResult.Succeeded(request, "Capability tool completed."));
        }
    }

    private sealed class ApprovalRequiredBridgeTool : IBridgeTool
    {
        public const string ToolId = "fake.approvalRequired";

        public BridgeToolDescriptor Descriptor { get; } = new BridgeToolDescriptor
        {
            Id = ToolId,
            Name = "Fake Approval Required",
            Description = "Fake approval-required test tool.",
            Category = "Tests",
            Source = "Compiled",
            Host = "SharedTests",
            ApprovalRequired = true
        };

        public BridgeToolRequest? LastRequest { get; private set; }

        public Task<BridgeToolResult> ExecuteAsync(BridgeToolRequest request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(BridgeToolResult.Succeeded(request, "Approval-required tool completed."));
        }
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

    private sealed class RecordingCapabilityPolicy : IToolExecutionPolicy
    {
        private readonly ToolExecutionPolicyDecision _decision;

        public RecordingCapabilityPolicy(ToolExecutionPolicyDecision decision)
        {
            _decision = decision;
        }

        public int CallCount { get; private set; }

        public ToolExecutionSecurityContext? LastContext { get; private set; }

        public Task<ToolExecutionPolicyDecision> EvaluateAsync(ToolExecutionSecurityContext context, CancellationToken cancellationToken)
        {
            CallCount++;
            LastContext = context;
            return Task.FromResult(_decision);
        }
    }

    private sealed class RecordingToolExecutionApprovalService : IToolExecutionApprovalService
    {
        private readonly ToolExecutionApprovalDecision _decision;

        public RecordingToolExecutionApprovalService(ToolExecutionApprovalDecision decision)
        {
            _decision = decision;
        }

        public int CallCount { get; private set; }

        public ToolExecutionApprovalContext? LastContext { get; private set; }

        public Task<ToolExecutionApprovalDecision> EvaluateAsync(ToolExecutionApprovalContext context, CancellationToken cancellationToken)
        {
            CallCount++;
            LastContext = context;
            return Task.FromResult(_decision);
        }
    }

    private sealed class DuplicateFakeBridgeTool : IBridgeTool
    {
        public BridgeToolDescriptor Descriptor { get; } = new BridgeToolDescriptor
        {
            Id = "fake.echo",
            Name = "Duplicate Fake Echo",
            Description = "Duplicate fake test tool."
        };

        public Task<BridgeToolResult> ExecuteAsync(BridgeToolRequest request, CancellationToken cancellationToken)
            => Task.FromResult(BridgeToolResult.Succeeded(request, "Duplicate completed."));
    }
}

[Export(typeof(IBridgeTool))]
public sealed class MefFakeBridgeTool : IBridgeTool
{
    public const string ToolId = "fake.mef";

    public static int ExecutionCount { get; set; }

    public BridgeToolDescriptor Descriptor { get; } = new BridgeToolDescriptor
    {
        Id = ToolId,
        Name = "Fake MEF Tool",
        Description = "Fake MEF-discovered test tool.",
        Category = "Tests",
        Source = "MEF",
        Host = "SharedTests"
    };

    public Task<BridgeToolResult> ExecuteAsync(BridgeToolRequest request, CancellationToken cancellationToken)
    {
        ExecutionCount++;
        return Task.FromResult(BridgeToolResult.Succeeded(request, "MEF completed."));
    }
}
