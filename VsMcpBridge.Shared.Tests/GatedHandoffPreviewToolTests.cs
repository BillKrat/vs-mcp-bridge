using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
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

public sealed class GatedHandoffPreviewToolTests
{
    [Fact]
    public void Compiled_catalog_discovers_gated_handoff_preview_tool()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILogger, RecordingBridgeLogger>();
        services.AddBridgeToolServices();
        using var provider = services.BuildServiceProvider();

        var descriptor = Assert.Single(
            provider.GetRequiredService<IBridgeToolCatalog>().GetTools(),
            tool => tool.Id == GatedHandoffPreviewTool.ToolId);

        Assert.Equal("Gated Handoff Preview", descriptor.Name);
        Assert.Equal("CodexHandoffPreview", descriptor.Category);
        Assert.Equal(new[] { "codex.gatedHandoffPreview" }, descriptor.RequiredCapabilities.Select(capability => capability.Name).ToArray());
        Assert.Equal(ToolExecutionApprovalRequirement.NotRequired, descriptor.ApprovalRequirement);
        Assert.Equal(AuditEventCategory.CodexHandoffPreview, descriptor.Manifest.RiskProfile.AuditCategoryHint);
    }

    [Fact]
    public async Task ExecuteAsync_returns_structured_preview_with_scoped_task_and_validation_checklist()
    {
        var tool = new GatedHandoffPreviewTool();
        var request = CreateRequest(new Dictionary<string, object?>
        {
            ["sliceObjective"] = "Implement a preview-only gated handoff proposal contract.",
            ["repoPath"] = @"\\Mac\Dev\vs-mcp-bridge",
            ["constraints"] = new[] { "No runtime code outside the compiled bridge tool.", "No Codex execution." },
            ["nonGoals"] = new[] { "No deployment." },
            ["validationRequirements"] = new[] { "git diff --check", "dotnet test ./VsMcpBridge.Shared.Tests/VsMcpBridge.Shared.Tests.csproj" },
            ["expectedArtifacts"] = new[] { "Compiled bridge tool contract", "Focused tests" }
        });

        var result = await tool.ExecuteAsync(request, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(GatedHandoffPreviewStatus.PreviewGenerated.ToString(), result.Data["status"]);
        Assert.Equal("request-123", result.Data["correlationId"]);
        Assert.Equal("operation-456", result.Data["operationId"]);
        Assert.Equal("Implement a preview-only gated handoff proposal contract.", result.Data["taskSummary"]);
        Assert.Contains("Scoped gated handoff proposal", Assert.IsType<string>(result.Data["scopedTaskText"]));
        Assert.Equal(new[] { "git diff --check", "dotnet test ./VsMcpBridge.Shared.Tests/VsMcpBridge.Shared.Tests.csproj" }, ToStrings(result.Data["validationChecklist"]));
        Assert.True(Assert.IsType<bool>(result.Data["approvalRequiredBeforeExecution"]));
        Assert.Contains("User approval is required", Assert.IsType<string>(result.Data["approvalReminder"]));
    }

    [Fact]
    public async Task ExecuteAsync_does_not_run_commands_or_mutate_repo_state()
    {
        var root = Path.Combine(Path.GetTempPath(), "VsMcpBridgeGatedPreviewTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var before = Directory.GetFileSystemEntries(root);
        var tool = new GatedHandoffPreviewTool();
        var request = CreateRequest(new Dictionary<string, object?>
        {
            ["sliceObjective"] = "Preview a handoff for a docs-only slice.",
            ["repoPath"] = root,
            ["validationRequirements"] = new[] { "git diff --check" }
        });

        try
        {
            var result = await tool.ExecuteAsync(request, CancellationToken.None);

            Assert.True(result.Success);
            Assert.False(Assert.IsType<bool>(result.Data["codexExecutionInvoked"]));
            Assert.False(Assert.IsType<bool>(result.Data["commandExecutionPerformed"]));
            Assert.False(Assert.IsType<bool>(result.Data["repoMutationPerformed"]));
            Assert.False(Assert.IsType<bool>(result.Data["deploymentPerformed"]));
            Assert.False(Assert.IsType<bool>(result.Data["backgroundWorkflowStarted"]));
            Assert.Equal(before, Directory.GetFileSystemEntries(root));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_redacts_secret_shaped_values_and_flags_risky_scope()
    {
        var tool = new GatedHandoffPreviewTool();
        var request = CreateRequest(new Dictionary<string, object?>
        {
            ["sliceObjective"] = "Deploy production auth and invoke Codex to edit files, commit, push, run git reset --hard, and run PowerShell command.",
            ["repoPath"] = @"\\Mac\Dev\vs-mcp-bridge",
            ["constraints"] = new[] { "Use password=raw-password", "Authorization: Bearer raw-bearer-token" },
            ["validationRequirements"] = new[] { "git diff --check" },
            ["deploymentRestrictions"] = new[] { "No WebDeploy without approval." },
            ["riskFlags"] = new[] { "ManualReview" }
        });

        var result = await tool.ExecuteAsync(request, CancellationToken.None);

        Assert.True(result.Success);
        var flattened = FlattenData(result.Data);
        Assert.DoesNotContain(flattened, value => value.Contains("raw-password", StringComparison.Ordinal));
        Assert.DoesNotContain(flattened, value => value.Contains("raw-bearer-token", StringComparison.Ordinal));
        Assert.Contains(flattened, value => value.Contains("[REDACTED]", StringComparison.Ordinal));
        Assert.True(Assert.IsType<bool>(result.Data["redactionApplied"]));

        var riskFlags = ToStrings(result.Data["riskFlags"]);
        Assert.Contains("Deployment", riskFlags);
        Assert.Contains("DestructiveGit", riskFlags);
        Assert.Contains("ProductionAuth", riskFlags);
        Assert.Contains("SecretHandling", riskFlags);
        Assert.Contains("CommandExecution", riskFlags);
        Assert.Contains("RepoMutation", riskFlags);
        Assert.Contains("CodexExecution", riskFlags);
        Assert.Contains("ManualReview", riskFlags);
        Assert.Contains(ToStrings(result.Data["stopConditions"]), condition => condition.Contains("destructive git", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ExecuteAsync_generates_missing_correlation_metadata_and_handles_missing_optional_inputs()
    {
        var tool = new GatedHandoffPreviewTool();
        var request = new BridgeToolRequest
        {
            ToolId = GatedHandoffPreviewTool.ToolId,
            Arguments = new Dictionary<string, object?>
            {
                ["sliceObjective"] = "Preview a minimal docs-only handoff.",
                ["repoPath"] = @"\\Mac\Dev\vs-mcp-bridge",
                ["validationRequirements"] = "git diff --check",
                ["unknownOptionalInput"] = new { Ignored = true }
            }
        };

        var result = await tool.ExecuteAsync(request, CancellationToken.None);

        Assert.True(result.Success);
        Assert.False(string.IsNullOrWhiteSpace(request.RequestId));
        Assert.False(string.IsNullOrWhiteSpace(request.OperationId));
        Assert.Equal(request.RequestId, result.Data["correlationId"]);
        Assert.Equal(Array.Empty<string>(), ToStrings(result.Data["constraints"]));
        Assert.Equal(Array.Empty<string>(), ToStrings(result.Data["nonGoals"]));
        Assert.Equal(new[] { "git diff --check" }, ToStrings(result.Data["validationChecklist"]));
    }

    [Fact]
    public async Task ExecuteAsync_returns_safe_failure_for_missing_required_inputs()
    {
        var tool = new GatedHandoffPreviewTool();
        var request = CreateRequest(new Dictionary<string, object?>
        {
            ["sliceObjective"] = "Preview an incomplete request.",
            ["repoPath"] = @"\\Mac\Dev\vs-mcp-bridge"
        });

        var result = await tool.ExecuteAsync(request, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("InvalidRequest", result.ErrorCode);
        Assert.Equal(GatedHandoffPreviewStatus.InvalidRequest.ToString(), result.Data["status"]);
        Assert.False(Assert.IsType<bool>(result.Data["codexExecutionInvoked"]));
        Assert.False(Assert.IsType<bool>(result.Data["repoMutationPerformed"]));
    }

    [Fact]
    public async Task ExecuteAsync_preserves_correlation_metadata_and_audit_category_through_executor()
    {
        var logger = new RecordingBridgeLogger();
        var auditSink = new InMemoryAuditSink();
        var executor = new BridgeToolExecutor(
            new CompiledBridgeToolCatalog(new IBridgeTool[] { new GatedHandoffPreviewTool() }),
            logger,
            new BridgeSecurityRedactor(),
            auditSink,
            new AllowToolExecutionPolicy());
        var request = CreateRequest(new Dictionary<string, object?>
        {
            ["sliceObjective"] = "Preview a gated handoff.",
            ["repoPath"] = @"\\Mac\Dev\vs-mcp-bridge",
            ["validationRequirements"] = new[] { "git diff --check" }
        });

        var result = await executor.ExecuteAsync(request, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("request-123", result.RequestId);
        Assert.Equal("operation-456", result.OperationId);
        Assert.Equal("request-123", result.Data["correlationId"]);
        var auditEvent = Assert.Single(auditSink.Events);
        Assert.Equal(GatedHandoffPreviewTool.ToolId, auditEvent.ToolId);
        Assert.Equal("request-123", auditEvent.RequestId);
        Assert.Equal("operation-456", auditEvent.OperationId);
        Assert.Equal(AuditEventCategory.CodexHandoffPreview, auditEvent.Category);
        Assert.Equal("CodexHandoffPreview", auditEvent.Metadata["manifestAuditCategoryHint"]);
    }

    private static BridgeToolRequest CreateRequest(IReadOnlyDictionary<string, object?> arguments)
        => new BridgeToolRequest
        {
            ToolId = GatedHandoffPreviewTool.ToolId,
            RequestId = "request-123",
            OperationId = "operation-456",
            Arguments = arguments
        };

    private static IReadOnlyList<string> ToStrings(object? value)
    {
        if (value is IReadOnlyList<string> strings)
            return strings;

        if (value is IEnumerable enumerable && value is not string)
            return enumerable.Cast<object?>().Select(item => item?.ToString() ?? string.Empty).ToArray();

        if (value is string text)
            return new[] { text };

        return Array.Empty<string>();
    }

    private static IReadOnlyList<string> FlattenData(IReadOnlyDictionary<string, object?> data)
    {
        var values = new List<string>();
        foreach (var value in data.Values)
            CollectStrings(value, values);

        return values;
    }

    private static void CollectStrings(object? value, ICollection<string> values)
    {
        if (value == null)
            return;

        if (value is string text)
        {
            values.Add(text);
            return;
        }

        if (value is IEnumerable enumerable)
        {
            foreach (var item in enumerable)
                CollectStrings(item, values);
        }
    }
}
