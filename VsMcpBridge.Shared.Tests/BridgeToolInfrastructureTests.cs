using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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
        Assert.IsType<BridgeToolInventoryService>(provider.GetRequiredService<IBridgeToolInventoryService>());
        Assert.IsType<BridgeToolExecutor>(provider.GetRequiredService<IBridgeToolExecutor>());
        Assert.IsType<BridgeSecurityRedactor>(provider.GetRequiredService<ISecurityRedactor>());
        Assert.IsType<NoOpAuditSink>(provider.GetRequiredService<IAuditSink>());
        Assert.IsType<AllowToolExecutionPolicy>(provider.GetRequiredService<IToolExecutionPolicy>());
        Assert.IsType<AllowToolExecutionApprovalService>(provider.GetRequiredService<IToolExecutionApprovalService>());
        Assert.IsType<NoOpSecretBroker>(provider.GetRequiredService<ISecretBroker>());
        Assert.Contains(provider.GetRequiredService<IBridgeToolCatalog>().GetTools(), tool => tool.Id == RegexTextSearchTool.ToolId);
        Assert.Contains(provider.GetRequiredService<IBridgeToolCatalog>().GetTools(), tool => tool.Id == Bm25TextSearchTool.ToolId);
        Assert.Contains(provider.GetRequiredService<IBridgeToolCatalog>().GetTools(), tool => tool.Id == PreviewDocumentUpdateTool.ToolId);
        Assert.All(
            provider.GetRequiredService<IBridgeToolCatalog>().GetTools().Where(tool => tool.Id != PreviewDocumentUpdateTool.ToolId),
            tool => Assert.Empty(tool.RequiredCapabilities));
        var previewTool = Assert.Single(provider.GetRequiredService<IBridgeToolCatalog>().GetTools(), tool => tool.Id == PreviewDocumentUpdateTool.ToolId);
        Assert.Equal(new[] { "workspace.previewDocumentUpdate" }, previewTool.RequiredCapabilities.Select(capability => capability.Name).ToArray());
        Assert.Equal(ToolExecutionApprovalRequirement.NotRequired, previewTool.ApprovalRequirement);
        Assert.All(provider.GetRequiredService<IBridgeToolCatalog>().GetTools(), tool => Assert.Equal(BridgeToolManifest.DefaultVersion, tool.Manifest.Identity.Version));
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
        Assert.IsType<BridgeToolInventoryService>(provider.GetRequiredService<IBridgeToolInventoryService>());
        Assert.IsType<BridgeToolExecutor>(provider.GetRequiredService<IBridgeToolExecutor>());
        Assert.IsType<BridgeSecurityRedactor>(provider.GetRequiredService<ISecurityRedactor>());
        Assert.IsType<NoOpAuditSink>(provider.GetRequiredService<IAuditSink>());
        Assert.IsType<AllowToolExecutionPolicy>(provider.GetRequiredService<IToolExecutionPolicy>());
        Assert.IsType<AllowToolExecutionApprovalService>(provider.GetRequiredService<IToolExecutionApprovalService>());
        Assert.IsType<NoOpSecretBroker>(provider.GetRequiredService<ISecretBroker>());
        Assert.Contains(provider.GetRequiredService<IBridgeToolCatalog>().GetTools(), tool => tool.Id == RegexTextSearchTool.ToolId);
        Assert.Contains(provider.GetRequiredService<IBridgeToolCatalog>().GetTools(), tool => tool.Id == Bm25TextSearchTool.ToolId);
        Assert.Contains(provider.GetRequiredService<IBridgeToolCatalog>().GetTools(), tool => tool.Id == PreviewDocumentUpdateTool.ToolId);
        Assert.All(
            provider.GetRequiredService<IBridgeToolCatalog>().GetTools().Where(tool => tool.Id != PreviewDocumentUpdateTool.ToolId),
            tool => Assert.Empty(tool.RequiredCapabilities));
        Assert.All(provider.GetRequiredService<IBridgeToolCatalog>().GetTools(), tool => Assert.True(tool.Manifest.Execution.ExecutesThroughBridgeToolExecutor));
    }

    [Fact]
    public void Tool_inventory_exposes_compiled_tool_manifest_metadata()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILogger, RecordingBridgeLogger>();
        services.AddBridgeToolServices();
        using var provider = services.BuildServiceProvider();
        var inventory = provider.GetRequiredService<IBridgeToolInventoryService>();

        var snapshot = inventory.GetSnapshot();

        var regex = Assert.Single(snapshot.Tools, tool => tool.Id == RegexTextSearchTool.ToolId);
        Assert.Equal("Regex Text Search", regex.Name);
        Assert.Equal(BridgeToolManifest.DefaultVersion, regex.Version);
        Assert.Equal("Searches supplied text entries with a regular expression.", regex.Description);
        Assert.Equal("Search", regex.Category);
        Assert.Equal("Compiled", regex.Source);
        Assert.Equal(BridgeToolDiscoveryKind.Compiled, regex.DiscoveryKind);
        Assert.Equal("Shared", regex.HostAffinity);
        Assert.True(regex.IsHostSpecific);
        Assert.Empty(regex.RequiredCapabilities);
        Assert.Equal(ToolExecutionApprovalRequirement.NotRequired, regex.ApprovalRequirement);
        Assert.Equal(AuditEventCategory.ToolExecution, regex.AuditCategoryHint);
        Assert.Equal(AuditSeverity.Informational, regex.SeverityHint);
        Assert.Equal(AuditRiskLevel.Low, regex.RiskLevelHint);
        Assert.True(regex.ExecutesThroughBridgeToolExecutor);
        Assert.Contains(snapshot.Tools, tool => tool.Id == Bm25TextSearchTool.ToolId);
        var preview = Assert.Single(snapshot.Tools, tool => tool.Id == PreviewDocumentUpdateTool.ToolId);
        Assert.Equal("Preview Document Update", preview.Name);
        Assert.Equal("DocumentPreview", preview.Category);
        Assert.Equal(new[] { "workspace.previewDocumentUpdate" }, preview.RequiredCapabilities);
        Assert.Equal(ToolExecutionApprovalRequirement.NotRequired, preview.ApprovalRequirement);
        Assert.Equal(AuditEventCategory.DocumentPreview, preview.AuditCategoryHint);
    }

    [Fact]
    public void Tool_inventory_ordering_is_deterministic_by_tool_id()
    {
        var tools = new[]
        {
            new InventoryProbeBridgeTool("fake.zeta"),
            new InventoryProbeBridgeTool("fake.alpha"),
            new InventoryProbeBridgeTool("fake.middle")
        };
        var inventory = new BridgeToolInventoryService(new CompiledBridgeToolCatalog(tools));

        var snapshot = inventory.GetSnapshot();

        Assert.Equal(new[] { "fake.alpha", "fake.middle", "fake.zeta" }, snapshot.Tools.Select(tool => tool.Id).ToArray());
    }

    [Fact]
    public void Tool_inventory_does_not_execute_tools()
    {
        var tool = new InventoryProbeBridgeTool(
            "fake.inventory",
            requiredCapabilities: new[] { new BridgeCapability("workspace.read"), new BridgeCapability("diagnostics.read") },
            approvalRequirement: ToolExecutionApprovalRequirement.Required);
        var inventory = new BridgeToolInventoryService(new CompiledBridgeToolCatalog(new IBridgeTool[] { tool }));

        var snapshot = inventory.GetSnapshot();

        Assert.Equal(0, tool.ExecutionCount);
        var item = Assert.Single(snapshot.Tools);
        Assert.Equal("fake.inventory", item.Id);
        Assert.Equal(new[] { "diagnostics.read", "workspace.read" }, item.RequiredCapabilities);
        Assert.Equal(ToolExecutionApprovalRequirement.Required, item.ApprovalRequirement);
    }

    [Fact]
    public void Tool_inventory_includes_mef_discovered_tool_metadata_when_enabled()
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
        var inventory = provider.GetRequiredService<IBridgeToolInventoryService>();

        var snapshot = inventory.GetSnapshot();

        var item = Assert.Single(snapshot.Tools, tool => tool.Id == MefFakeBridgeTool.ToolId);
        Assert.Equal("Fake MEF Tool", item.Name);
        Assert.Equal(BridgeToolManifest.DefaultVersion, item.Version);
        Assert.Equal("Tests", item.Category);
        Assert.Equal("MEF", item.Source);
        Assert.Equal(BridgeToolDiscoveryKind.Mef, item.DiscoveryKind);
        Assert.Equal("SharedTests", item.HostAffinity);
        Assert.Equal(ToolExecutionApprovalRequirement.NotRequired, item.ApprovalRequirement);
        Assert.Equal(0, MefFakeBridgeTool.ExecutionCount);
    }

    [Fact]
    public void Descriptor_derives_stable_manifest_defaults()
    {
        var descriptor = new BridgeToolDescriptor
        {
            Id = "fake.default",
            Name = "Fake Default"
        };

        var manifest = descriptor.Manifest;

        Assert.Equal("fake.default", manifest.Identity.Id);
        Assert.Equal("Fake Default", manifest.Identity.Name);
        Assert.Equal(BridgeToolManifest.DefaultVersion, manifest.Identity.Version);
        Assert.Equal(string.Empty, manifest.Description);
        Assert.Equal(string.Empty, manifest.Category);
        Assert.Equal(BridgeToolDiscoveryKind.Unspecified, manifest.Execution.DiscoveryKind);
        Assert.Equal(string.Empty, manifest.Execution.Source);
        Assert.True(manifest.Execution.ExecutesThroughBridgeToolExecutor);
        Assert.Empty(manifest.RequiredCapabilities);
        Assert.Equal(ToolExecutionApprovalRequirement.NotRequired, manifest.ApprovalRequirement);
        Assert.Equal(AuditEventCategory.ToolExecution, manifest.RiskProfile.AuditCategoryHint);
        Assert.Equal(AuditSeverity.Informational, manifest.RiskProfile.SeverityHint);
        Assert.Equal(AuditRiskLevel.Low, manifest.RiskProfile.RiskLevelHint);
        Assert.False(manifest.HostAffinity.IsHostSpecific);
    }

    [Fact]
    public void Descriptor_manifest_exposes_identity_capability_approval_risk_and_host_metadata()
    {
        var capabilities = new[] { new BridgeCapability("workspace.read") };
        var descriptor = new BridgeToolDescriptor
        {
            Id = "fake.manifest",
            Name = "Fake Manifest",
            Version = "2.1.0",
            Description = "Manifest metadata test.",
            Category = "Tests",
            Source = "MEF",
            Host = "SharedTests",
            RequiredCapabilities = capabilities,
            ApprovalRequirement = ToolExecutionApprovalRequirement.Required,
            RiskProfile = new BridgeToolRiskProfile
            {
                AuditCategoryHint = AuditEventCategory.Approval,
                SeverityHint = AuditSeverity.Warning,
                RiskLevelHint = AuditRiskLevel.Medium
            }
        };

        var manifest = descriptor.Manifest;

        Assert.Equal("fake.manifest", manifest.Identity.Id);
        Assert.Equal("Fake Manifest", manifest.Identity.Name);
        Assert.Equal("2.1.0", manifest.Identity.Version);
        Assert.Equal("Manifest metadata test.", manifest.Description);
        Assert.Equal("Tests", manifest.Category);
        Assert.Equal("MEF", manifest.Execution.Source);
        Assert.Equal(BridgeToolDiscoveryKind.Mef, manifest.Execution.DiscoveryKind);
        Assert.Same(capabilities, manifest.RequiredCapabilities);
        Assert.Equal(ToolExecutionApprovalRequirement.Required, manifest.ApprovalRequirement);
        Assert.Equal(AuditEventCategory.Approval, manifest.RiskProfile.AuditCategoryHint);
        Assert.Equal(AuditSeverity.Warning, manifest.RiskProfile.SeverityHint);
        Assert.Equal(AuditRiskLevel.Medium, manifest.RiskProfile.RiskLevelHint);
        Assert.Equal("SharedTests", manifest.HostAffinity.Host);
        Assert.True(manifest.HostAffinity.IsHostSpecific);
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
        var descriptor = Assert.Single(catalog.GetTools(), tool => tool.Id == MefFakeBridgeTool.ToolId);
        Assert.Equal("1.0.0", descriptor.Manifest.Identity.Version);
        Assert.Equal("Tests", descriptor.Manifest.Category);
        Assert.Equal("MEF", descriptor.Manifest.Execution.Source);
        Assert.Equal(BridgeToolDiscoveryKind.Mef, descriptor.Manifest.Execution.DiscoveryKind);
        Assert.Equal("SharedTests", descriptor.Manifest.HostAffinity.Host);
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
        AssertAuditClassification(
            auditEvent,
            AuditEventCategory.Policy,
            AuditSeverity.Warning,
            AuditRiskLevel.Medium,
            AuditOutcome.Denied);
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
        Assert.Equal("fake.echo", auditEvent.Metadata["manifestToolId"]);
        Assert.Equal("Fake Echo", auditEvent.Metadata["manifestToolName"]);
        Assert.Equal(BridgeToolManifest.DefaultVersion, auditEvent.Metadata["manifestVersion"]);
        Assert.Equal("Tests", auditEvent.Metadata["manifestCategory"]);
        Assert.Equal("Compiled", auditEvent.Metadata["manifestSource"]);
        Assert.Equal("Compiled", auditEvent.Metadata["manifestDiscoveryKind"]);
        Assert.Equal("SharedTests", auditEvent.Metadata["manifestHost"]);
        Assert.Equal("NotRequired", auditEvent.Metadata["manifestApprovalRequirement"]);
        Assert.Equal("ToolExecution", auditEvent.Metadata["manifestAuditCategoryHint"]);
        Assert.Equal("Informational", auditEvent.Metadata["manifestSeverityHint"]);
        Assert.Equal("Low", auditEvent.Metadata["manifestRiskLevel"]);
        AssertAuditClassification(
            auditEvent,
            AuditEventCategory.ToolExecution,
            AuditSeverity.Informational,
            AuditRiskLevel.Low,
            AuditOutcome.Succeeded);
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
        AssertAuditClassification(
            auditEvent,
            AuditEventCategory.ToolExecution,
            AuditSeverity.Informational,
            AuditRiskLevel.Low,
            AuditOutcome.Succeeded);
    }

    [Fact]
    public async Task Executor_flows_secret_references_to_policy_context_without_raw_secret_values()
    {
        var logger = new RecordingBridgeLogger();
        var auditSink = new InMemoryAuditSink();
        var policy = new RecordingSecretAwarePolicy(ToolExecutionPolicyDecision.Allow("secret refs inspected"));
        var broker = new RecordingSecretBroker(SecretResolutionResult.ResolvedReference("Synthetic reference resolved"));
        var tool = new FakeBridgeTool();
        var executor = new BridgeToolExecutor(
            new CompiledBridgeToolCatalog(new[] { tool }),
            logger,
            new BridgeSecurityRedactor(),
            auditSink,
            policy,
            new AllowToolExecutionApprovalService(),
            broker);
        var request = CreateRequestWithSecretReference("fake.echo", new SecretReference("openai-api-key", SecretReferenceKind.Named, "test-provider"));

        var result = await executor.ExecuteAsync(request, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Same(request, tool.LastRequest);
        Assert.Equal(1, policy.CallCount);
        Assert.Equal(new[] { "openai-api-key" }, policy.LastContext!.SecretReferences.Select(reference => reference.ReferenceId).ToArray());
        Assert.Equal(1, broker.CallCount);
        Assert.Equal("openai-api-key", broker.LastReference!.ReferenceId);
        var auditEvent = Assert.Single(auditSink.Events);
        Assert.True(auditEvent.Allowed);
        Assert.True(auditEvent.Success);
        Assert.Equal("Named:test-provider:openai-api-key", auditEvent.Metadata["secretReferences"]);
        Assert.Equal("Secret references resolved", auditEvent.Metadata["secretResolution"]);
        Assert.Equal("request-123", auditEvent.RequestId);
        Assert.Equal("operation-456", auditEvent.OperationId);
        AssertAuditClassification(
            auditEvent,
            AuditEventCategory.ToolExecution,
            AuditSeverity.Informational,
            AuditRiskLevel.Low,
            AuditOutcome.Succeeded);
    }

    [Fact]
    public async Task Executor_returns_structured_failure_for_unresolved_secret_reference()
    {
        var logger = new RecordingBridgeLogger();
        var auditSink = new InMemoryAuditSink();
        var policy = new RecordingSecretAwarePolicy(ToolExecutionPolicyDecision.Allow("policy allowed"));
        var broker = new RecordingSecretBroker(SecretResolutionResult.Unresolved("missing secret=raw-broker-secret"));
        var tool = new FakeBridgeTool();
        var executor = new BridgeToolExecutor(
            new CompiledBridgeToolCatalog(new[] { tool }),
            logger,
            new BridgeSecurityRedactor(),
            auditSink,
            policy,
            new AllowToolExecutionApprovalService(),
            broker);
        var request = CreateRequestWithSecretReference("fake.echo", new SecretReference("token=raw-reference-secret", SecretReferenceKind.Named));

        var result = await executor.ExecuteAsync(request, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("SecretReferenceUnresolved", result.ErrorCode);
        Assert.Contains("token=[REDACTED]", result.Message);
        Assert.Contains("secret=[REDACTED]", result.Message);
        Assert.Null(tool.LastRequest);
        Assert.Equal(1, policy.CallCount);
        Assert.Equal(1, broker.CallCount);
        Assert.DoesNotContain(logger.VerboseMessages, message => message.Contains("raw-reference-secret"));
        Assert.DoesNotContain(logger.WarningMessages, message => message.Contains("raw-broker-secret"));
        var auditEvent = Assert.Single(auditSink.Events);
        Assert.False(auditEvent.Allowed);
        Assert.False(auditEvent.Success);
        Assert.Equal("SecretReferenceUnresolved", auditEvent.ErrorCode);
        Assert.Equal("Named:token=[REDACTED]", auditEvent.Metadata["secretReferences"]);
        Assert.Contains("secret=[REDACTED]", auditEvent.Metadata["secretResolution"]);
        Assert.DoesNotContain(auditEvent.Metadata["secretReferences"], "raw-reference-secret");
        Assert.DoesNotContain(auditEvent.Metadata["secretResolution"], "raw-broker-secret");
        Assert.Equal("request-123", auditEvent.RequestId);
        Assert.Equal("operation-456", auditEvent.OperationId);
        AssertAuditClassification(
            auditEvent,
            AuditEventCategory.Secret,
            AuditSeverity.Warning,
            AuditRiskLevel.High,
            AuditOutcome.Failed);
    }

    [Fact]
    public async Task Executor_runs_policy_and_approval_before_unresolved_secret_reference_failure()
    {
        var logger = new RecordingBridgeLogger();
        var auditSink = new InMemoryAuditSink();
        var policy = new RecordingSecretAwarePolicy(ToolExecutionPolicyDecision.Allow("policy allowed"));
        var approvalService = new RecordingToolExecutionApprovalService(ToolExecutionApprovalDecision.Approve("operator approved"));
        var broker = new RecordingSecretBroker(SecretResolutionResult.Unresolved("No synthetic secret"));
        var tool = new ApprovalRequiredBridgeTool();
        var executor = new BridgeToolExecutor(
            new CompiledBridgeToolCatalog(new[] { tool }),
            logger,
            new BridgeSecurityRedactor(),
            auditSink,
            policy,
            approvalService,
            broker);
        var request = CreateRequestWithSecretReference(ApprovalRequiredBridgeTool.ToolId, new SecretReference("openai-api-key", SecretReferenceKind.Named));

        var result = await executor.ExecuteAsync(request, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("SecretReferenceUnresolved", result.ErrorCode);
        Assert.Null(tool.LastRequest);
        Assert.Equal(1, policy.CallCount);
        Assert.Equal(1, approvalService.CallCount);
        Assert.Equal(1, broker.CallCount);
        var auditEvent = Assert.Single(auditSink.Events);
        Assert.Equal("Approved", auditEvent.Metadata["approvalDecision"]);
        Assert.Equal("Named:openai-api-key", auditEvent.Metadata["secretReferences"]);
        Assert.Equal("request-123", auditEvent.RequestId);
        Assert.Equal("operation-456", auditEvent.OperationId);
        AssertAuditClassification(
            auditEvent,
            AuditEventCategory.Secret,
            AuditSeverity.Warning,
            AuditRiskLevel.High,
            AuditOutcome.Failed);
    }

    [Fact]
    public async Task Capability_policy_allows_tool_with_no_required_capabilities()
    {
        var policy = new CapabilityToolExecutionPolicy(new CapabilityToolExecutionPolicyOptions
        {
            DenyUnknownRequiredCapabilities = true
        });
        var request = CreateRequest("fake.echo");
        var descriptor = new BridgeToolDescriptor
        {
            Id = "fake.echo",
            Name = "Fake Echo"
        };

        var decision = await policy.EvaluateAsync(new ToolExecutionSecurityContext(request, descriptor), CancellationToken.None);

        Assert.True(decision.Allowed);
        Assert.Equal("No required capabilities", decision.Reason);
    }

    [Fact]
    public async Task Capability_policy_allows_configured_required_capability()
    {
        var options = new CapabilityToolExecutionPolicyOptions
        {
            DenyUnknownRequiredCapabilities = true
        };
        options.AllowedCapabilities.Add("workspace.read");
        var policy = new CapabilityToolExecutionPolicy(options);
        var request = CreateRequest("fake.capability");
        var descriptor = new BridgeToolDescriptor
        {
            Id = "fake.capability",
            Name = "Fake Capability",
            RequiredCapabilities = new[] { new BridgeCapability("workspace.read") }
        };

        var decision = await policy.EvaluateAsync(new ToolExecutionSecurityContext(request, descriptor), CancellationToken.None);

        Assert.True(decision.Allowed);
        Assert.Equal("Required capabilities allowed", decision.Reason);
    }

    [Fact]
    public async Task Capability_policy_denies_configured_denied_capability()
    {
        var options = new CapabilityToolExecutionPolicyOptions();
        options.AllowedCapabilities.Add("workspace.write");
        options.DeniedCapabilities.Add("workspace.write");
        var policy = new CapabilityToolExecutionPolicy(options);
        var request = CreateRequest("fake.capability");
        var descriptor = new BridgeToolDescriptor
        {
            Id = "fake.capability",
            Name = "Fake Capability",
            RequiredCapabilities = new[] { new BridgeCapability("workspace.write") }
        };

        var decision = await policy.EvaluateAsync(new ToolExecutionSecurityContext(request, descriptor), CancellationToken.None);

        Assert.False(decision.Allowed);
        Assert.Equal("Denied required capability 'workspace.write'.", decision.Reason);
    }

    [Fact]
    public async Task Capability_policy_unknown_capability_behavior_is_configurable()
    {
        var request = CreateRequest("fake.capability");
        var descriptor = new BridgeToolDescriptor
        {
            Id = "fake.capability",
            Name = "Fake Capability",
            RequiredCapabilities = new[] { new BridgeCapability("workspace.unknown") }
        };
        var allowUnknown = new CapabilityToolExecutionPolicy(new CapabilityToolExecutionPolicyOptions());
        var denyUnknown = new CapabilityToolExecutionPolicy(new CapabilityToolExecutionPolicyOptions
        {
            DenyUnknownRequiredCapabilities = true
        });

        var allowed = await allowUnknown.EvaluateAsync(new ToolExecutionSecurityContext(request, descriptor), CancellationToken.None);
        var denied = await denyUnknown.EvaluateAsync(new ToolExecutionSecurityContext(request, descriptor), CancellationToken.None);

        Assert.True(allowed.Allowed);
        Assert.Equal("Required capabilities allowed", allowed.Reason);
        Assert.False(denied.Allowed);
        Assert.Equal("Unknown required capability 'workspace.unknown'.", denied.Reason);
    }

    [Fact]
    public async Task Executor_uses_capability_policy_to_deny_and_preserves_audit_metadata()
    {
        var logger = new RecordingBridgeLogger();
        var auditSink = new InMemoryAuditSink();
        var options = new CapabilityToolExecutionPolicyOptions();
        options.DeniedCapabilities.Add("workspace.write");
        var policy = new CapabilityToolExecutionPolicy(options);
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
        Assert.Equal("Denied required capability 'workspace.write'.", result.Message);
        Assert.Null(tool.LastRequest);
        var auditEvent = Assert.Single(auditSink.Events);
        Assert.False(auditEvent.Allowed);
        Assert.False(auditEvent.Success);
        Assert.Equal("PolicyDenied", auditEvent.ErrorCode);
        Assert.Equal("Denied required capability 'workspace.write'.", auditEvent.Metadata["policyReason"]);
        Assert.Equal("workspace.write", auditEvent.Metadata["requiredCapabilities"]);
        Assert.Equal("request-123", auditEvent.RequestId);
        Assert.Equal("operation-456", auditEvent.OperationId);
        AssertAuditClassification(
            auditEvent,
            AuditEventCategory.Policy,
            AuditSeverity.Warning,
            AuditRiskLevel.Medium,
            AuditOutcome.Denied);
    }

    [Fact]
    public async Task Executor_does_not_invoke_approval_when_capability_policy_denies_first()
    {
        var logger = new RecordingBridgeLogger();
        var auditSink = new InMemoryAuditSink();
        var approvalService = new RecordingToolExecutionApprovalService(ToolExecutionApprovalDecision.Approve("should not be called"));
        var options = new CapabilityToolExecutionPolicyOptions();
        options.DeniedCapabilities.Add("workspace.write");
        var policy = new CapabilityToolExecutionPolicy(options);
        var tool = new ApprovalRequiredBridgeTool
        {
            Descriptor =
            {
                RequiredCapabilities = new[] { new BridgeCapability("workspace.write") }
            }
        };
        var executor = new BridgeToolExecutor(
            new CompiledBridgeToolCatalog(new[] { tool }),
            logger,
            new BridgeSecurityRedactor(),
            auditSink,
            policy,
            approvalService);
        var request = CreateRequest(ApprovalRequiredBridgeTool.ToolId);

        var result = await executor.ExecuteAsync(request, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("PolicyDenied", result.ErrorCode);
        Assert.Null(tool.LastRequest);
        Assert.Equal(0, approvalService.CallCount);
        var auditEvent = Assert.Single(auditSink.Events);
        Assert.False(auditEvent.Allowed);
        Assert.Equal("NotEvaluated", auditEvent.Metadata["approvalDecision"]);
        Assert.Equal("workspace.write", auditEvent.Metadata["requiredCapabilities"]);
        Assert.Equal("request-123", auditEvent.RequestId);
        Assert.Equal("operation-456", auditEvent.OperationId);
        AssertAuditClassification(
            auditEvent,
            AuditEventCategory.Policy,
            AuditSeverity.Warning,
            AuditRiskLevel.Medium,
            AuditOutcome.Denied);
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
        AssertAuditClassification(
            auditEvent,
            AuditEventCategory.Policy,
            AuditSeverity.Warning,
            AuditRiskLevel.Medium,
            AuditOutcome.Denied);
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
        AssertAuditClassification(
            auditEvent,
            AuditEventCategory.ToolExecution,
            AuditSeverity.Informational,
            AuditRiskLevel.Low,
            AuditOutcome.Succeeded);
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
        AssertAuditClassification(
            auditEvent,
            AuditEventCategory.ToolExecution,
            AuditSeverity.Informational,
            AuditRiskLevel.Low,
            AuditOutcome.Succeeded);
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
        AssertAuditClassification(
            auditEvent,
            AuditEventCategory.Approval,
            AuditSeverity.Informational,
            AuditRiskLevel.Medium,
            AuditOutcome.Denied);
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
        AssertAuditClassification(
            auditEvent,
            AuditEventCategory.Execution,
            AuditSeverity.Error,
            AuditRiskLevel.High,
            AuditOutcome.Failed);
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
        AssertAuditClassification(
            auditEvent,
            AuditEventCategory.Policy,
            AuditSeverity.Warning,
            AuditRiskLevel.Medium,
            AuditOutcome.Denied);
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

    [Fact]
    public async Task Preview_document_update_generates_deterministic_unified_diff_without_mutating_file()
    {
        var root = CreatePreviewFixtureRoot();
        try
        {
            var path = Path.Combine(root, "docs", "example.md");
            File.WriteAllText(path, "alpha\nbravo\n");
            var executor = CreatePreviewExecutor(root, new RecordingBridgeLogger());

            var result = await executor.ExecuteAsync(
                CreatePreviewRequest("docs/example.md", expectedContent: "alpha\nbravo\n", replacementContent: "alpha\ncharlie\n"),
                CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(PreviewDocumentUpdateTool.ToolId, result.ToolId);
            Assert.Equal("request-123", result.RequestId);
            Assert.Equal("operation-456", result.OperationId);
            Assert.Equal("PreviewGenerated", result.Data["status"]);
            Assert.Equal(true, result.Data["previewOnly"]);
            Assert.Equal(false, result.Data["noOp"]);
            Assert.Equal(4, result.Data["changedLineCount"]);
            Assert.Equal(
                "--- a/docs/example.md\n+++ b/docs/example.md\n@@ -1,2 +1,2 @@\n-alpha\n-bravo\n+alpha\n+charlie\n",
                result.Data["diff"]);
            Assert.Equal("alpha\nbravo\n", File.ReadAllText(path));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Preview_document_update_detects_no_op_without_mutating_file()
    {
        var root = CreatePreviewFixtureRoot();
        try
        {
            var path = Path.Combine(root, "docs", "example.md");
            File.WriteAllText(path, "same\n");
            var executor = CreatePreviewExecutor(root, new RecordingBridgeLogger());

            var result = await executor.ExecuteAsync(
                CreatePreviewRequest("docs/example.md", expectedContent: "same\n", replacementContent: "same\n"),
                CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal("NoOp", result.Data["status"]);
            Assert.Equal(true, result.Data["noOp"]);
            Assert.Equal(string.Empty, result.Data["diff"]);
            Assert.Equal("same\n", File.ReadAllText(path));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Preview_document_update_accepts_expected_content_hash()
    {
        var root = CreatePreviewFixtureRoot();
        try
        {
            var path = Path.Combine(root, "docs", "hash.md");
            File.WriteAllText(path, "current\n");
            var executor = CreatePreviewExecutor(root, new RecordingBridgeLogger());

            var result = await executor.ExecuteAsync(
                CreatePreviewRequest(
                    "docs/hash.md",
                    expectedContent: null,
                    replacementContent: "replacement\n",
                    expectedContentHash: ComputeSha256("current\n")),
                CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal("PreviewGenerated", result.Data["status"]);
            Assert.Equal("hash", result.Data["expectedStateMode"]);
            Assert.Equal("current\n", File.ReadAllText(path));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Preview_document_update_detects_drift_without_mutating_file()
    {
        var root = CreatePreviewFixtureRoot();
        try
        {
            var path = Path.Combine(root, "docs", "example.md");
            File.WriteAllText(path, "current\n");
            var executor = CreatePreviewExecutor(root, new RecordingBridgeLogger());

            var result = await executor.ExecuteAsync(
                CreatePreviewRequest("docs/example.md", expectedContent: "expected\n", replacementContent: "replacement\n"),
                CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("DriftDetected", result.ErrorCode);
            Assert.Equal("DriftDetected", result.Data["status"]);
            Assert.Equal(false, result.Data["expectedMatched"]);
            Assert.Equal("current\n", File.ReadAllText(path));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Theory]
    [InlineData("../outside.md")]
    [InlineData("docs/*.md")]
    [InlineData("C:/temp/file.md")]
    public async Task Preview_document_update_rejects_invalid_paths(string targetPath)
    {
        var root = CreatePreviewFixtureRoot();
        try
        {
            File.WriteAllText(Path.Combine(root, "docs", "example.md"), "current\n");
            var executor = CreatePreviewExecutor(root, new RecordingBridgeLogger());

            var result = await executor.ExecuteAsync(
                CreatePreviewRequest(targetPath, expectedContent: "current\n", replacementContent: "replacement\n"),
                CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("InvalidTargetPath", result.ErrorCode);
            Assert.Equal("InvalidRequest", result.Data["status"]);
            Assert.Equal("current\n", File.ReadAllText(Path.Combine(root, "docs", "example.md")));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Preview_document_update_uses_executor_audit_redaction_and_no_approval_by_default()
    {
        var root = CreatePreviewFixtureRoot();
        try
        {
            File.WriteAllText(Path.Combine(root, "docs", "secret.md"), "token=raw-current-secret\n");
            var logger = new RecordingBridgeLogger();
            var auditSink = new InMemoryAuditSink();
            var policy = new RecordingCapabilityPolicy(ToolExecutionPolicyDecision.Allow("preview observed"));
            var executor = new BridgeToolExecutor(
                new CompiledBridgeToolCatalog(new IBridgeTool[] { new PreviewDocumentUpdateTool(root) }),
                logger,
                new BridgeSecurityRedactor(),
                auditSink,
                policy);

            var result = await executor.ExecuteAsync(
                CreatePreviewRequest("docs/secret.md", expectedContent: "token=raw-current-secret\n", replacementContent: "token=raw-result-secret\n"),
                CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal("PreviewGenerated", result.Data["status"]);
            Assert.Equal(1, policy.CallCount);
            Assert.Equal(PreviewDocumentUpdateTool.ToolId, policy.LastContext!.ToolId);
            Assert.Equal(ToolExecutionApprovalRequirement.NotRequired, policy.LastContext.Manifest!.ApprovalRequirement);

            var auditEvent = Assert.Single(auditSink.Events);
            Assert.True(auditEvent.Success);
            Assert.True(auditEvent.Allowed);
            Assert.Equal(AuditEventCategory.DocumentPreview, auditEvent.Category);
            Assert.Equal(AuditSeverity.Informational, auditEvent.Severity);
            Assert.Equal(AuditRiskLevel.Low, auditEvent.RiskLevel);
            Assert.Equal(AuditOutcome.Succeeded, auditEvent.Outcome);
            Assert.Equal("Preview Document Update", auditEvent.Metadata["manifestToolName"]);
            Assert.Equal("DocumentPreview", auditEvent.Metadata["auditCategory"]);
            Assert.Equal("NotRequired", auditEvent.Metadata["approvalDecision"]);

            var logMessages = logger.VerboseMessages
                .Concat(logger.InformationMessages)
                .Concat(logger.WarningMessages)
                .Concat(logger.Errors.Select(error => error.Message))
                .Concat(logger.Errors.Select(error => error.Exception?.Message ?? string.Empty))
                .ToArray();
            Assert.DoesNotContain(logMessages, message => message.Contains("raw-current-secret", StringComparison.Ordinal));
            Assert.DoesNotContain(logMessages, message => message.Contains("raw-result-secret", StringComparison.Ordinal));
            Assert.Contains(logMessages, message => message.Contains("[REDACTED]", StringComparison.Ordinal));
            Assert.DoesNotContain(auditEvent.Metadata.Values, value => value.Contains("raw-current-secret", StringComparison.Ordinal));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    private static BridgeToolRequest CreateRequest(string toolId)
        => new BridgeToolRequest
        {
            ToolId = toolId,
            RequestId = "request-123",
            OperationId = "operation-456",
            Arguments = new Dictionary<string, object?> { ["input"] = "hello" }
        };

    private static string CreatePreviewFixtureRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "vs-mcp-bridge-preview-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "docs"));
        return root;
    }

    private static BridgeToolExecutor CreatePreviewExecutor(string root, RecordingBridgeLogger logger)
        => new BridgeToolExecutor(
            new CompiledBridgeToolCatalog(new IBridgeTool[] { new PreviewDocumentUpdateTool(root) }),
            logger);

    private static BridgeToolRequest CreatePreviewRequest(
        string targetPath,
        string? expectedContent,
        string replacementContent,
        string? expectedContentHash = null)
    {
        var arguments = new Dictionary<string, object?>
        {
            ["targetPath"] = targetPath,
            ["replacementContent"] = replacementContent
        };

        if (expectedContent != null)
            arguments["expectedContent"] = expectedContent;

        if (expectedContentHash != null)
            arguments["expectedContentHash"] = expectedContentHash;

        return new BridgeToolRequest
        {
            ToolId = PreviewDocumentUpdateTool.ToolId,
            RequestId = "request-123",
            OperationId = "operation-456",
            Arguments = arguments
        };
    }

    private static string ComputeSha256(string text)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(text));
        var builder = new StringBuilder(bytes.Length * 2);
        foreach (var value in bytes)
            builder.Append(value.ToString("x2"));

        return builder.ToString();
    }

    private static void AssertAuditClassification(
        BridgeAuditEnvelope auditEvent,
        AuditEventCategory category,
        AuditSeverity severity,
        AuditRiskLevel riskLevel,
        AuditOutcome outcome)
    {
        Assert.Equal(category, auditEvent.Category);
        Assert.Equal(severity, auditEvent.Severity);
        Assert.Equal(riskLevel, auditEvent.RiskLevel);
        Assert.Equal(outcome, auditEvent.Outcome);
        Assert.Equal(category.ToString(), auditEvent.Metadata["auditCategory"]);
        Assert.Equal(severity.ToString(), auditEvent.Metadata["auditSeverity"]);
        Assert.Equal(riskLevel.ToString(), auditEvent.Metadata["auditRiskLevel"]);
        Assert.Equal(outcome.ToString(), auditEvent.Metadata["auditOutcome"]);
    }

    private static BridgeToolRequest CreateRequestWithSecretReference(string toolId, ISecretReference secretReference)
        => new BridgeToolRequest
        {
            ToolId = toolId,
            RequestId = "request-123",
            OperationId = "operation-456",
            Arguments = new Dictionary<string, object?>
            {
                ["input"] = "hello",
                ["apiKey"] = secretReference
            }
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

    private sealed class InventoryProbeBridgeTool : IBridgeTool
    {
        public InventoryProbeBridgeTool(
            string toolId,
            IReadOnlyList<BridgeCapability>? requiredCapabilities = null,
            ToolExecutionApprovalRequirement approvalRequirement = ToolExecutionApprovalRequirement.NotRequired)
        {
            Descriptor = new BridgeToolDescriptor
            {
                Id = toolId,
                Name = $"Inventory Probe {toolId}",
                Version = "2.0.0-test",
                Description = "Fake inventory metadata test tool.",
                Category = "Tests",
                Source = "Compiled",
                Host = "SharedTests",
                RequiredCapabilities = requiredCapabilities ?? Array.Empty<BridgeCapability>(),
                ApprovalRequirement = approvalRequirement,
                RiskProfile = new BridgeToolRiskProfile
                {
                    AuditCategoryHint = AuditEventCategory.ToolExecution,
                    SeverityHint = AuditSeverity.Warning,
                    RiskLevelHint = AuditRiskLevel.Medium
                }
            };
        }

        public BridgeToolDescriptor Descriptor { get; }

        public int ExecutionCount { get; private set; }

        public Task<BridgeToolResult> ExecuteAsync(BridgeToolRequest request, CancellationToken cancellationToken)
        {
            ExecutionCount++;
            return Task.FromResult(BridgeToolResult.Succeeded(request, "Inventory probe completed."));
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

    private sealed class RecordingSecretAwarePolicy : IToolExecutionPolicy
    {
        private readonly ToolExecutionPolicyDecision _decision;

        public RecordingSecretAwarePolicy(ToolExecutionPolicyDecision decision)
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

    private sealed class RecordingSecretBroker : ISecretBroker
    {
        private readonly SecretResolutionResult _result;

        public RecordingSecretBroker(SecretResolutionResult result)
        {
            _result = result;
        }

        public int CallCount { get; private set; }

        public ISecretReference? LastReference { get; private set; }

        public Task<SecretResolutionResult> ResolveAsync(ISecretReference reference, CancellationToken cancellationToken)
        {
            CallCount++;
            LastReference = reference;
            return Task.FromResult(_result);
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
