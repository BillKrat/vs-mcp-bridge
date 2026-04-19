using Microsoft.Extensions.DependencyInjection;
using System;
using VsMcpBridge.Shared.Interfaces;
using VsMcpBridge.Shared.Loggers;
using VsMcpBridge.Shared.Models;
using VsMcpBridge.Shared.MvpVm;
using VsMcpBridge.Shared.Services;
using VsMcpBridge.Vsix.Services;
using VsMcpBridge.Vsix.Tests.Support;
using VsMcpBridge.Shared.Tests.Support;
using Xunit;

namespace VsMcpBridge.Vsix.Tests;

public sealed class VsServiceTests
{
    private static IServiceProvider CreateServiceProvider(IVsService vsService)
    {
        return new ServiceCollection()
            .AddSingleton(vsService)
            .BuildServiceProvider();
    }

    [Fact]
    public void Constructor_logs_bridge_service_startup()
    {
        var logger = new RecordingBridgeLogger();
        IThreadHelper threadHelper = new TestThreadHelper();
        var viewModel = new LogToolWindowViewModel();
        var presenter = new LogToolWindowPresenter(CreateServiceProvider(new StubVsService()), logger, threadHelper, viewModel);
        _ = new VsService(TestPackageFactory.CreatePackage(), logger, threadHelper, new InMemoryApprovalWorkflowService(), new RecordingEditApplier(), presenter);

        Assert.Contains("Bridge service startup complete.", logger.VerboseMessages);
    }

    [Fact]
    public async System.Threading.Tasks.Task ProposeTextEditAsync_returns_empty_diff_when_text_is_unchanged()
    {
        var logger = new RecordingBridgeLogger();
        IThreadHelper threadHelper = new TestThreadHelper();
        var viewModel = new LogToolWindowViewModel();
        var presenter = new LogToolWindowPresenter(CreateServiceProvider(new StubVsService()), logger, threadHelper, viewModel);
        var workflow = new InMemoryApprovalWorkflowService();
        var service = new VsService(TestPackageFactory.CreatePackage(), logger, threadHelper, workflow, new RecordingEditApplier(), presenter);

        var response = await service.ProposeTextEditAsync("request-123", "sample.cs", "same", "same");

        Assert.True(response.Success);
        Assert.Equal("request-123", response.RequestId);
        Assert.Equal(string.Empty, response.Diff);
        Assert.Empty(TestWorkflowHelpers.GetProposalIds(workflow));
        Assert.False(viewModel.HasPendingApproval);
        Assert.Equal(string.Empty, viewModel.PendingApprovalDescription);
        Assert.Contains(logger.InformationMessages, message => message.Contains("Generating proposed diff for 'sample.cs' [RequestId=request-123]"));
        Assert.Contains(logger.InformationMessages, message => message.Contains("No edit proposal created because the proposed text matches the original text [RequestId=request-123]"));
    }

    [Fact]
    public async System.Threading.Tasks.Task ProposeTextEditAsync_routes_created_proposal_into_presenter_view_model()
    {
        var logger = new RecordingBridgeLogger();
        IThreadHelper threadHelper = new TestThreadHelper();
        var viewModel = new LogToolWindowViewModel();
        var presenter = new LogToolWindowPresenter(CreateServiceProvider(new StubVsService()), logger, threadHelper, viewModel);
        var workflow = new InMemoryApprovalWorkflowService();
        var service = new VsService(TestPackageFactory.CreatePackage(), logger, threadHelper, workflow, new RecordingEditApplier(), presenter);

        var response = await service.ProposeTextEditAsync("request-123", "sample.cs", "before", "after");

        Assert.True(response.Success);
        Assert.Equal("request-123", response.RequestId);
        Assert.Contains("--- a/sample.cs", response.Diff);
        Assert.Contains("+++ b/sample.cs", response.Diff);
        Assert.Contains("-before", response.Diff);
        Assert.Contains("+after", response.Diff);
        Assert.True(viewModel.HasPendingApproval);
        Assert.Contains("sample.cs", viewModel.PendingApprovalDescription);
        Assert.Contains("--- a/sample.cs", viewModel.PendingApprovalDescription);
        Assert.True(viewModel.ApproveCommand.CanExecute(null));
        Assert.True(viewModel.RejectCommand.CanExecute(null));
        var proposalId = Assert.Single(TestWorkflowHelpers.GetProposalIds(workflow));
        Assert.Contains(logger.InformationMessages, message => message.Contains($"Created edit proposal [RequestId=request-123] [ProposalId={proposalId}]"));
        Assert.Contains(logger.InformationMessages, message => message.Contains($"Proposal pending approval [RequestId=request-123] [ProposalId={proposalId}]"));
    }

    [Fact]
    public async System.Threading.Tasks.Task ProposeTextEditsAsync_routes_created_multi_file_proposal_into_presenter_view_model()
    {
        var logger = new RecordingBridgeLogger();
        IThreadHelper threadHelper = new TestThreadHelper();
        var viewModel = new LogToolWindowViewModel();
        var presenter = new LogToolWindowPresenter(CreateServiceProvider(new StubVsService()), logger, threadHelper, viewModel);
        var workflow = new InMemoryApprovalWorkflowService();
        var service = new VsService(TestPackageFactory.CreatePackage(), logger, threadHelper, workflow, new RecordingEditApplier(), presenter);
        var fileEdits = new[]
        {
            new ProposalFileEditRequest { FilePath = "first.cs", OriginalText = "before-1", ProposedText = "after-1" },
            new ProposalFileEditRequest { FilePath = "second.cs", OriginalText = "before-2", ProposedText = "after-2" }
        };

        var response = await service.ProposeTextEditsAsync("request-multi", fileEdits);

        Assert.True(response.Success);
        Assert.Equal("request-multi", response.RequestId);
        Assert.Contains("--- a/first.cs", response.Diff);
        Assert.Contains("--- a/second.cs", response.Diff);
        Assert.True(viewModel.HasPendingApproval);
        Assert.Contains("Pending proposal for 2 files", viewModel.PendingApprovalDescription);
        Assert.Equal(2, viewModel.PendingApprovalIncludedFiles.Count);
        Assert.Equal("first.cs", viewModel.PendingApprovalIncludedFiles[0]);
        Assert.Equal("second.cs", viewModel.PendingApprovalIncludedFiles[1]);
        var proposalId = Assert.Single(TestWorkflowHelpers.GetProposalIds(workflow));
        Assert.Contains(logger.InformationMessages, message => message.Contains($"Created edit proposal [RequestId=request-multi] [ProposalId={proposalId}] for 2 files.", StringComparison.Ordinal));
        Assert.Contains(logger.InformationMessages, message => message.Contains($"Proposal pending approval [RequestId=request-multi] [ProposalId={proposalId}] for 2 files.", StringComparison.Ordinal));
    }

    [Fact]
    public async System.Threading.Tasks.Task ProposeTextEditAsync_approve_command_applies_edit_marks_proposal_applied_and_clears_pending_state()
    {
        var logger = new RecordingBridgeLogger();
        IThreadHelper threadHelper = new TestThreadHelper();
        var viewModel = new LogToolWindowViewModel();
        var presenter = new LogToolWindowPresenter(CreateServiceProvider(new StubVsService()), logger, threadHelper, viewModel);
        var workflow = new InMemoryApprovalWorkflowService();
        var editApplier = new RecordingEditApplier();
        var service = new VsService(TestPackageFactory.CreatePackage(), logger, threadHelper, workflow, editApplier, presenter);

        await service.ProposeTextEditAsync("request-123", "sample.cs", "before", "after");

        var proposalId = Assert.Single(TestWorkflowHelpers.GetProposalIds(workflow));
        viewModel.ApproveCommand.Execute(null);

        Assert.Single(editApplier.AppliedProposals);
        Assert.Equal("request-123", editApplier.AppliedProposals[0].RequestId);
        Assert.Equal(proposalId, editApplier.AppliedProposals[0].ProposalId);
        Assert.Equal(ProposalStatus.Applied, workflow.Get(proposalId)!.Status);
        Assert.False(viewModel.HasPendingApproval);
        Assert.Equal(string.Empty, viewModel.PendingApprovalDescription);
        Assert.Equal("Apply succeeded for 1 file. All approved changes were applied.", viewModel.StatusMessage);
        Assert.False(viewModel.ApproveCommand.CanExecute(null));
        Assert.False(viewModel.RejectCommand.CanExecute(null));
        Assert.False(viewModel.IsProposalProposedTextReadOnly);
        Assert.Contains(logger.InformationMessages, message => message.Contains($"Proposal approved [RequestId=request-123] [ProposalId={proposalId}]"));
        Assert.Contains(logger.InformationMessages, message => message.Contains($"Apply succeeded [RequestId=request-123] [ProposalId={proposalId}]"));
    }

    [Fact]
    public async System.Threading.Tasks.Task ProposeTextEditAsync_approve_command_logs_skip_when_target_already_matches_updated_content()
    {
        var logger = new RecordingBridgeLogger();
        IThreadHelper threadHelper = new TestThreadHelper();
        var viewModel = new LogToolWindowViewModel();
        var presenter = new LogToolWindowPresenter(CreateServiceProvider(new StubVsService()), logger, threadHelper, viewModel);
        var workflow = new InMemoryApprovalWorkflowService();
        var editApplier = new SkippingEditApplier();
        var service = new VsService(TestPackageFactory.CreatePackage(), logger, threadHelper, workflow, editApplier, presenter);

        await service.ProposeTextEditAsync("request-123", "sample.cs", "before", "after");

        var proposalId = Assert.Single(TestWorkflowHelpers.GetProposalIds(workflow));
        viewModel.ApproveCommand.Execute(null);

        Assert.Equal(1, editApplier.Calls);
        Assert.Equal(ProposalStatus.Applied, workflow.Get(proposalId)!.Status);
        Assert.False(viewModel.HasPendingApproval);
        Assert.Equal(string.Empty, viewModel.PendingApprovalDescription);
        Assert.Equal("Apply skipped for 1 file because all targets already match the approved content.", viewModel.StatusMessage);
        Assert.False(viewModel.ApproveCommand.CanExecute(null));
        Assert.False(viewModel.RejectCommand.CanExecute(null));
        Assert.False(viewModel.IsProposalProposedTextReadOnly);
        Assert.Contains(logger.InformationMessages, message => message.Contains("Apply skipped for 1 file because all targets already match the approved content.", StringComparison.Ordinal) && message.Contains("RequestId=request-123") && message.Contains($"ProposalId={proposalId}"));
    }

    [Fact]
    public async System.Threading.Tasks.Task ProposeTextEditAsync_reject_command_does_not_apply_and_marks_proposal_rejected()
    {
        var logger = new RecordingBridgeLogger();
        IThreadHelper threadHelper = new TestThreadHelper();
        var viewModel = new LogToolWindowViewModel();
        var presenter = new LogToolWindowPresenter(CreateServiceProvider(new StubVsService()), logger, threadHelper, viewModel);
        var workflow = new InMemoryApprovalWorkflowService();
        var editApplier = new RecordingEditApplier();
        var service = new VsService(TestPackageFactory.CreatePackage(), logger, threadHelper, workflow, editApplier, presenter);

        await service.ProposeTextEditAsync("request-123", "sample.cs", "before", "after");

        var proposalId = Assert.Single(TestWorkflowHelpers.GetProposalIds(workflow));
        viewModel.RejectCommand.Execute(null);

        Assert.Empty(editApplier.AppliedProposals);
        Assert.Equal(ProposalStatus.Rejected, workflow.Get(proposalId)!.Status);
        Assert.False(viewModel.HasPendingApproval);
        Assert.Equal(string.Empty, viewModel.PendingApprovalDescription);
        Assert.Equal("Proposal rejected for 1 file. No changes were applied.", viewModel.StatusMessage);
        Assert.False(viewModel.ApproveCommand.CanExecute(null));
        Assert.False(viewModel.RejectCommand.CanExecute(null));
        Assert.False(viewModel.IsProposalProposedTextReadOnly);
        Assert.Contains(logger.InformationMessages, message => message.Contains($"Proposal rejected [RequestId=request-123] [ProposalId={proposalId}]"));
    }

    [Fact]
    public async System.Threading.Tasks.Task ProposeTextEditAsync_approve_command_marks_proposal_failed_when_apply_fails()
    {
        var logger = new RecordingBridgeLogger();
        IThreadHelper threadHelper = new TestThreadHelper();
        var viewModel = new LogToolWindowViewModel();
        var presenter = new LogToolWindowPresenter(CreateServiceProvider(new StubVsService()), logger, threadHelper, viewModel);
        var workflow = new InMemoryApprovalWorkflowService();
        var editApplier = new ThrowingEditApplier();
        var service = new VsService(TestPackageFactory.CreatePackage(), logger, threadHelper, workflow, editApplier, presenter);

        await service.ProposeTextEditAsync("request-123", "sample.cs", "before", "after");

        var proposalId = Assert.Single(TestWorkflowHelpers.GetProposalIds(workflow));
        viewModel.ApproveCommand.Execute(null);

        Assert.Equal(1, editApplier.Calls);
        Assert.Equal(ProposalStatus.Failed, workflow.Get(proposalId)!.Status);
        Assert.False(viewModel.HasPendingApproval);
        Assert.Equal(string.Empty, viewModel.PendingApprovalDescription);
        Assert.Equal("Apply failed for 1 file. No changes were applied. Review the bridge log for details.", viewModel.StatusMessage);
        Assert.False(viewModel.ApproveCommand.CanExecute(null));
        Assert.False(viewModel.RejectCommand.CanExecute(null));
        Assert.False(viewModel.IsProposalProposedTextReadOnly);
        Assert.Contains(logger.InformationMessages, message => message.Contains($"Proposal approved [RequestId=request-123] [ProposalId={proposalId}]"));
        Assert.Contains(logger.Errors, error => error.Message.Contains($"Apply failed [RequestId=request-123] [ProposalId={proposalId}]"));
    }

    [Fact]
    public async System.Threading.Tasks.Task ProposeTextEditAsync_approve_command_marks_proposal_failed_when_target_document_has_drifted()
    {
        var logger = new RecordingBridgeLogger();
        IThreadHelper threadHelper = new TestThreadHelper();
        var viewModel = new LogToolWindowViewModel();
        var presenter = new LogToolWindowPresenter(CreateServiceProvider(new StubVsService()), logger, threadHelper, viewModel);
        var workflow = new InMemoryApprovalWorkflowService();
        var editApplier = new DriftingEditApplier();
        var service = new VsService(TestPackageFactory.CreatePackage(), logger, threadHelper, workflow, editApplier, presenter);

        await service.ProposeTextEditAsync("request-123", "sample.cs", "before", "after");

        var proposalId = Assert.Single(TestWorkflowHelpers.GetProposalIds(workflow));
        viewModel.ApproveCommand.Execute(null);

        Assert.Equal(1, editApplier.Calls);
        Assert.Equal(ProposalStatus.Failed, workflow.Get(proposalId)!.Status);
        Assert.False(viewModel.HasPendingApproval);
        Assert.Equal(string.Empty, viewModel.PendingApprovalDescription);
        Assert.Equal("Apply failed for 1 file because at least one target no longer matches the approved content. No changes were applied.", viewModel.StatusMessage);
        Assert.False(viewModel.ApproveCommand.CanExecute(null));
        Assert.False(viewModel.RejectCommand.CanExecute(null));
        Assert.False(viewModel.IsProposalProposedTextReadOnly);
        Assert.Contains(logger.WarningMessages, message =>
            message.Contains("Apply failed for 1 file because at least one target no longer matches the approved content. No changes were applied.", StringComparison.Ordinal) &&
            message.Contains("RequestId=request-123") &&
            message.Contains($"ProposalId={proposalId}"));
        Assert.DoesNotContain(logger.Errors, error => error.Exception?.Message == "Target document no longer matches the approved proposal.");
    }

    [Fact]
    public async System.Threading.Tasks.Task ProposeTextEditAsync_approve_command_marks_proposal_failed_when_target_is_ambiguous()
    {
        var logger = new RecordingBridgeLogger();
        IThreadHelper threadHelper = new TestThreadHelper();
        var viewModel = new LogToolWindowViewModel();
        var presenter = new LogToolWindowPresenter(CreateServiceProvider(new StubVsService()), logger, threadHelper, viewModel);
        var workflow = new InMemoryApprovalWorkflowService();
        var editApplier = new AmbiguousEditApplier();
        var service = new VsService(TestPackageFactory.CreatePackage(), logger, threadHelper, workflow, editApplier, presenter);

        await service.ProposeTextEditAsync("request-123", "sample.cs", "before", "after");

        var proposalId = Assert.Single(TestWorkflowHelpers.GetProposalIds(workflow));
        viewModel.ApproveCommand.Execute(null);

        Assert.Equal(1, editApplier.Calls);
        Assert.Equal(ProposalStatus.Failed, workflow.Get(proposalId)!.Status);
        Assert.Equal("Apply failed for 1 file because at least one target location is ambiguous. No changes were applied.", viewModel.StatusMessage);
        Assert.Contains(logger.WarningMessages, message =>
            message.Contains("Apply failed for 1 file because at least one target location is ambiguous. No changes were applied.", StringComparison.Ordinal) &&
            message.Contains("RequestId=request-123") &&
            message.Contains($"ProposalId={proposalId}"));
    }

    [Fact]
    public async System.Threading.Tasks.Task ProposeTextEditsAsync_approve_command_reports_multi_file_success_with_file_count()
    {
        var logger = new RecordingBridgeLogger();
        IThreadHelper threadHelper = new TestThreadHelper();
        var viewModel = new LogToolWindowViewModel();
        var presenter = new LogToolWindowPresenter(CreateServiceProvider(new StubVsService()), logger, threadHelper, viewModel);
        var workflow = new InMemoryApprovalWorkflowService();
        var editApplier = new RecordingEditApplier();
        var service = new VsService(TestPackageFactory.CreatePackage(), logger, threadHelper, workflow, editApplier, presenter);
        var fileEdits = new[]
        {
            new ProposalFileEditRequest { FilePath = "first.cs", OriginalText = "before-1", ProposedText = "after-1" },
            new ProposalFileEditRequest { FilePath = "second.cs", OriginalText = "before-2", ProposedText = "after-2" }
        };

        await service.ProposeTextEditsAsync("request-multi", fileEdits);

        var proposalId = Assert.Single(TestWorkflowHelpers.GetProposalIds(workflow));
        viewModel.ApproveCommand.Execute(null);

        Assert.Equal("Apply succeeded for 2 files. All approved changes were applied.", viewModel.StatusMessage);
        Assert.Contains(logger.InformationMessages, message =>
            message.Contains("Apply succeeded for 2 files. All approved changes were applied.", StringComparison.Ordinal) &&
            message.Contains("RequestId=request-multi") &&
            message.Contains($"ProposalId={proposalId}"));
    }

    [Fact]
    public async System.Threading.Tasks.Task ProposeTextEditsAsync_approve_command_reports_proposal_wide_multi_file_failure()
    {
        var logger = new RecordingBridgeLogger();
        IThreadHelper threadHelper = new TestThreadHelper();
        var viewModel = new LogToolWindowViewModel();
        var presenter = new LogToolWindowPresenter(CreateServiceProvider(new StubVsService()), logger, threadHelper, viewModel);
        var workflow = new InMemoryApprovalWorkflowService();
        var editApplier = new DriftingEditApplier();
        var service = new VsService(TestPackageFactory.CreatePackage(), logger, threadHelper, workflow, editApplier, presenter);
        var fileEdits = new[]
        {
            new ProposalFileEditRequest { FilePath = "first.cs", OriginalText = "before-1", ProposedText = "after-1" },
            new ProposalFileEditRequest { FilePath = "second.cs", OriginalText = "before-2", ProposedText = "after-2" }
        };

        await service.ProposeTextEditsAsync("request-multi", fileEdits);

        var proposalId = Assert.Single(TestWorkflowHelpers.GetProposalIds(workflow));
        viewModel.ApproveCommand.Execute(null);

        Assert.Equal("Apply failed for 2 files because at least one target no longer matches the approved content. No changes were applied.", viewModel.StatusMessage);
        Assert.Contains(logger.WarningMessages, message =>
            message.Contains("Apply failed for 2 files because at least one target no longer matches the approved content. No changes were applied.", StringComparison.Ordinal) &&
            message.Contains("RequestId=request-multi") &&
            message.Contains($"ProposalId={proposalId}"));
    }
}
