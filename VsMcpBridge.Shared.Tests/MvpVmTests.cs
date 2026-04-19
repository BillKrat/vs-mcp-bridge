using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using VsMcpBridge.Shared.Composition;
using VsMcpBridge.Shared.Interfaces;
using VsMcpBridge.Shared.Loggers;
using VsMcpBridge.Shared.MvpVm;
using VsMcpBridge.Shared.Models;
using VsMcpBridge.Shared.Tests.Support;
using Xunit;

namespace VsMcpBridge.Shared.Tests;

public sealed class MvpVmTests
{
    private static IServiceProvider CreateServiceProvider(IVsService vsService)
    {
        return new ServiceCollection()
            .AddSingleton(vsService)
            .BuildServiceProvider();
    }

    private static IServiceProvider CreateServiceProvider(IVsService vsService, IProposalFilePicker proposalFilePicker)
    {
        return new ServiceCollection()
            .AddSingleton(vsService)
            .AddSingleton<IProposalFilePicker>(proposalFilePicker)
            .BuildServiceProvider();
    }

    [Fact]
    public void AddMvpVmServices_registers_shared_presenter_and_view_model()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILogger, RecordingBridgeLogger>();
        services.AddSingleton<IThreadHelper, TestThreadHelper>();
        services.AddSingleton<IVsService, StubVsService>();

        services.AddMvpVmServices();
        using var provider = services.BuildServiceProvider();

        Assert.IsType<LogToolWindowPresenter>(provider.GetRequiredService<ILogToolWindowPresenter>());
        Assert.IsType<LogToolWindowViewModel>(provider.GetRequiredService<ILogToolWindowViewModel>());
    }

    [Fact]
    public void Initialize_sets_data_context_and_wires_handlers()
    {
        var logger = new RecordingBridgeLogger();
        var threadHelper = new TestThreadHelper();
        var viewModel = new LogToolWindowViewModel();
        var presenter = new LogToolWindowPresenter(CreateServiceProvider(new StubVsService()), logger, threadHelper, viewModel);
        var control = new FakeLogToolWindowControl();

        presenter.LogToolWindowControl = control;

        presenter.Initialize();

        Assert.Same(viewModel, control.DataContext);
        Assert.Contains("Initializing VS MCP Bridge tool window...", logger.InformationMessages);
        Assert.Contains("VS MCP Bridge tool window Initialized.", logger.InformationMessages);
    }

    [Fact]
    public void AppendLog_replaces_initial_placeholder_and_appends_on_subsequent_calls()
    {
        var viewModel = new LogToolWindowViewModel();
        var presenter = new LogToolWindowPresenter(CreateServiceProvider(new StubVsService()), new RecordingBridgeLogger(), new TestThreadHelper(), viewModel);

        presenter.LogToolWindowControl = new FakeLogToolWindowControl();

        presenter.AppendLog("first");
        presenter.AppendLog("second");

        Assert.Equal($"first{System.Environment.NewLine}second", viewModel.LogText);
    }

    [Fact]
    public void ShowApprovalPrompt_updates_view_model_and_approve_command_invokes_callback()
    {
        var control = new FakeLogToolWindowControl();
        var viewModel = new LogToolWindowViewModel();
        var presenter = new LogToolWindowPresenter(CreateServiceProvider(new StubVsService()), new RecordingBridgeLogger(), new TestThreadHelper(), viewModel);
        var approved = false;
        var rejected = false;

        presenter.LogToolWindowControl = control;
        presenter.Initialize();

        presenter.ShowApprovalPrompt("Apply change", "before", "after", null, () => approved = true, () => rejected = true);

        Assert.True(viewModel.HasPendingApproval);
        Assert.Equal("Apply change", viewModel.PendingApprovalDescription);
        Assert.True(viewModel.HasPendingApprovalRangePreview);
        Assert.Equal("before", viewModel.PendingApprovalOriginalSegment);
        Assert.Equal("after", viewModel.PendingApprovalUpdatedSegment);
        Assert.True(viewModel.IsProposalOriginalTextReadOnly);
        Assert.True(viewModel.IsProposalProposedTextReadOnly);
        Assert.True(viewModel.ApproveCommand.CanExecute(null));

        viewModel.ApproveCommand.Execute(null);

        Assert.True(approved);
        Assert.False(rejected);
        Assert.True(viewModel.HasPendingApproval);
        Assert.Equal("Apply change", viewModel.PendingApprovalDescription);
    }

    [Fact]
    public void ProposalPane_read_only_state_matches_pending_approval_state()
    {
        var viewModel = new LogToolWindowViewModel();
        var presenter = new LogToolWindowPresenter(CreateServiceProvider(new StubVsService()), new RecordingBridgeLogger(), new TestThreadHelper(), viewModel);

        Assert.True(viewModel.IsProposalOriginalTextReadOnly);
        Assert.False(viewModel.IsProposalProposedTextReadOnly);
        Assert.False(viewModel.IsReviewFocusedLayoutActive);
        Assert.True(viewModel.ShowProposalEditor);
        Assert.True(viewModel.ShowSubmitProposalButton);

        presenter.ShowApprovalPrompt("Pending proposal", null, null, null, () => { }, () => { });

        Assert.True(viewModel.IsProposalOriginalTextReadOnly);
        Assert.True(viewModel.IsProposalProposedTextReadOnly);
        Assert.False(viewModel.HasPendingApprovalRangePreview);
        Assert.True(viewModel.IsReviewFocusedLayoutActive);
        Assert.True(viewModel.ShowProposalEditor);
        Assert.False(viewModel.ShowSubmitProposalButton);
    }

    [Fact]
    public void ApproveCommand_uses_pending_approval_callback_not_editable_proposal_fields()
    {
        var control = new FakeLogToolWindowControl();
        var viewModel = new LogToolWindowViewModel();
        var presenter = new LogToolWindowPresenter(CreateServiceProvider(new StubVsService()), new RecordingBridgeLogger(), new TestThreadHelper(), viewModel);
        var approved = false;

        presenter.LogToolWindowControl = control;
        presenter.Initialize();
        presenter.ShowApprovalPrompt("Pending proposal", "old segment", "new segment", null, () => approved = true, () => { });

        viewModel.ProposalFilePath = @"C:\repo\Different.cs";
        viewModel.ProposalOriginalText = "edited in proposal pane";
        viewModel.ProposalProposedText = "different proposed text";

        viewModel.ApproveCommand.Execute(null);

        Assert.True(approved);
        Assert.True(viewModel.HasPendingApproval);
        Assert.Equal("Pending proposal", viewModel.PendingApprovalDescription);
        Assert.Equal("old segment", viewModel.PendingApprovalOriginalSegment);
        Assert.Equal("new segment", viewModel.PendingApprovalUpdatedSegment);
    }

    [Fact]
    public void AppendLog_switches_to_main_thread_when_access_is_not_available()
    {
        var logger = new RecordingBridgeLogger();
        var threadHelper = new TestThreadHelper { HasAccess = false };
        var viewModel = new LogToolWindowViewModel();
        var presenter = new LogToolWindowPresenter(CreateServiceProvider(new StubVsService()), logger, threadHelper, viewModel)
        {
            LogToolWindowControl = new FakeLogToolWindowControl()
        };

        presenter.AppendLog("message");

        Assert.Equal(1, threadHelper.RunCalls);
        Assert.Equal(1, threadHelper.SwitchCalls);
        Assert.Equal("message", presenter.LogToolWindowViewModel.LogText);
    }

    [Fact]
    public void ShowApprovalPrompt_before_initialize_updates_shared_view_model_state()
    {
        var viewModel = new LogToolWindowViewModel();
        var presenter = new LogToolWindowPresenter(CreateServiceProvider(new StubVsService()), new RecordingBridgeLogger(), new TestThreadHelper(), viewModel);

        presenter.ShowApprovalPrompt("Pending proposal", null, null, null, () => { }, () => { });

        Assert.True(viewModel.HasPendingApproval);
        Assert.Equal("Pending proposal", viewModel.PendingApprovalDescription);
        Assert.True(viewModel.ApproveCommand.CanExecute(null));
        Assert.True(viewModel.RejectCommand.CanExecute(null));
    }

    [Fact]
    public void BrowseProposalFileCommand_updates_proposal_file_path_from_picker_selection()
    {
        var picker = new StubProposalFilePicker { SelectedPath = @"C:\repo\Sample.cs" };
        var viewModel = new LogToolWindowViewModel();
        _ = new LogToolWindowPresenter(CreateServiceProvider(new StubVsService(), picker), new RecordingBridgeLogger(), new TestThreadHelper(), viewModel);

        Assert.True(viewModel.BrowseProposalFileCommand.CanExecute(null));

        viewModel.BrowseProposalFileCommand.Execute(null);

        Assert.Equal(1, picker.Calls);
        Assert.Equal(@"C:\repo\Sample.cs", viewModel.ProposalFilePath);
        Assert.Single(viewModel.ProposalSelectedFiles);
    }

    [Fact]
    public void Add_and_remove_proposal_files_updates_selected_file_set_and_active_editor()
    {
        var firstPath = Path.GetTempFileName();
        var secondPath = Path.GetTempFileName();

        try
        {
            File.WriteAllText(firstPath, "first-before");
            File.WriteAllText(secondPath, "second-before");

            var picker = new StubProposalFilePicker { SelectedPath = firstPath };
            var viewModel = new LogToolWindowViewModel();
            _ = new LogToolWindowPresenter(CreateServiceProvider(new StubVsService(), picker), new RecordingBridgeLogger(), new TestThreadHelper(), viewModel);

            viewModel.BrowseProposalFileCommand.Execute(null);
            viewModel.ProposalProposedText = "first-after";
            viewModel.RequestInputText = "Update the selected files.";

            picker.SelectedPath = secondPath;
            viewModel.BrowseProposalFileCommand.Execute(null);

            Assert.Equal(2, viewModel.ProposalSelectedFiles.Count);
            Assert.Equal(secondPath, viewModel.ProposalFilePath);
            Assert.Equal("second-before", viewModel.ProposalOriginalText);
            Assert.Equal("second-before", viewModel.ProposalProposedText);

            viewModel.ProposalFilePath = firstPath;

            Assert.Equal("first-before", viewModel.ProposalOriginalText);
            Assert.Equal("first-after", viewModel.ProposalProposedText);

            viewModel.RemoveProposalFileCommand.Execute(null);

            Assert.Single(viewModel.ProposalSelectedFiles);
            Assert.Equal(secondPath, viewModel.ProposalFilePath);
            Assert.Equal("second-before", viewModel.ProposalOriginalText);
        }
        finally
        {
            File.Delete(firstPath);
            File.Delete(secondPath);
        }
    }

    [Fact]
    public void ResetProposalCommand_keeps_selected_files_and_per_file_drafts_while_clearing_request_state()
    {
        var firstPath = Path.GetTempFileName();
        var secondPath = Path.GetTempFileName();

        try
        {
            File.WriteAllText(firstPath, "first-before");
            File.WriteAllText(secondPath, "second-before");

            var picker = new StubProposalFilePicker { SelectedPath = firstPath };
            var viewModel = new LogToolWindowViewModel();
            _ = new LogToolWindowPresenter(CreateServiceProvider(new StubVsService(), picker), new RecordingBridgeLogger(), new TestThreadHelper(), viewModel);

            viewModel.BrowseProposalFileCommand.Execute(null);
            viewModel.ProposalProposedText = "first-after";
            viewModel.RequestInputText = "Update the selected files.";

            picker.SelectedPath = secondPath;
            viewModel.BrowseProposalFileCommand.Execute(null);
            viewModel.ProposalProposedText = "second-after";
            viewModel.LastSubmittedRequestText = "Update the selected files.";
            viewModel.IsRequestInProgress = true;

            Assert.Equal(2, viewModel.ProposalSelectedFiles.Count);
            Assert.True(viewModel.NewChatCommand.CanExecute(null));

            viewModel.ResetProposalCommand.Execute(null);

            Assert.Equal(2, viewModel.ProposalSelectedFiles.Count);
            Assert.Equal(secondPath, viewModel.ProposalFilePath);
            Assert.Equal("second-before", viewModel.ProposalOriginalText);
            Assert.Equal("second-after", viewModel.ProposalProposedText);
            Assert.True(viewModel.IsProposalFileLoaded);
            Assert.Equal(string.Empty, viewModel.RequestInputText);
            Assert.Equal(string.Empty, viewModel.LastSubmittedRequestText);
            Assert.False(viewModel.IsRequestInProgress);
        }
        finally
        {
            File.Delete(firstPath);
            File.Delete(secondPath);
        }
    }

    [Fact]
    public void NewChatCommand_clears_selected_files_and_per_file_drafts()
    {
        var firstPath = Path.GetTempFileName();
        var secondPath = Path.GetTempFileName();

        try
        {
            File.WriteAllText(firstPath, "first-before");
            File.WriteAllText(secondPath, "second-before");

            var picker = new StubProposalFilePicker { SelectedPath = firstPath };
            var viewModel = new LogToolWindowViewModel();
            _ = new LogToolWindowPresenter(CreateServiceProvider(new StubVsService(), picker), new RecordingBridgeLogger(), new TestThreadHelper(), viewModel);

            viewModel.BrowseProposalFileCommand.Execute(null);
            viewModel.ProposalProposedText = "first-after";

            picker.SelectedPath = secondPath;
            viewModel.BrowseProposalFileCommand.Execute(null);

            var exception = Record.Exception(() => viewModel.NewChatCommand.Execute(null));

            Assert.Null(exception);
            Assert.Empty(viewModel.ProposalSelectedFiles);
            Assert.False(viewModel.HasProposalDrafts);
            Assert.False(viewModel.HasLastCompletedProposalPreview);
            Assert.Equal(string.Empty, viewModel.ProposalFilePath);
        }
        finally
        {
            File.Delete(firstPath);
            File.Delete(secondPath);
        }
    }

    [Fact]
    public void ProposalSelectedFiles_setter_does_not_reenter_for_equivalent_values()
    {
        var viewModel = new LogToolWindowViewModel();
        var proposalSelectedFilesChangedCount = 0;

        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ILogToolWindowViewModel.ProposalSelectedFiles))
                proposalSelectedFilesChangedCount++;
        };

        viewModel.ProposalSelectedFiles = Array.Empty<string>();
        viewModel.ProposalSelectedFiles = Array.Empty<string>();
        viewModel.ProposalSelectedFiles = new[] { "first.cs", "second.cs" };
        viewModel.ProposalSelectedFiles = new[] { "first.cs", "second.cs" };
        viewModel.ProposalSelectedFiles = Array.Empty<string>();

        Assert.Equal(2, proposalSelectedFilesChangedCount);
        Assert.Empty(viewModel.ProposalSelectedFiles);
    }

    [Fact]
    public void ResetProposalCommand_is_disabled_when_only_selected_files_exist()
    {
        var path = Path.GetTempFileName();

        try
        {
            File.WriteAllText(path, "before");
            var picker = new StubProposalFilePicker { SelectedPath = path };
            var viewModel = new LogToolWindowViewModel();
            _ = new LogToolWindowPresenter(CreateServiceProvider(new StubVsService(), picker), new RecordingBridgeLogger(), new TestThreadHelper(), viewModel);

            Assert.False(viewModel.ResetProposalCommand.CanExecute(null));

            viewModel.BrowseProposalFileCommand.Execute(null);

            Assert.Single(viewModel.ProposalSelectedFiles);
            Assert.False(viewModel.ResetProposalCommand.CanExecute(null));
            Assert.True(viewModel.NewChatCommand.CanExecute(null));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void NewChatCommand_is_enabled_when_drafts_exist()
    {
        var path = Path.GetTempFileName();

        try
        {
            File.WriteAllText(path, "before");
            var picker = new StubProposalFilePicker { SelectedPath = path };
            var viewModel = new LogToolWindowViewModel();
            _ = new LogToolWindowPresenter(CreateServiceProvider(new StubVsService(), picker), new RecordingBridgeLogger(), new TestThreadHelper(), viewModel);

            viewModel.BrowseProposalFileCommand.Execute(null);
            viewModel.ProposalProposedText = "after";

            Assert.True(viewModel.HasProposalDrafts);
            Assert.False(viewModel.HasResettableProposalState);
            Assert.False(viewModel.ResetProposalCommand.CanExecute(null));
            Assert.True(viewModel.NewChatCommand.CanExecute(null));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ResetProposalCommand_restores_clean_proposal_entry_state()
    {
        var path = Path.GetTempFileName();

        try
        {
            File.WriteAllText(path, "before");
            var picker = new StubProposalFilePicker { SelectedPath = path };
            var viewModel = new LogToolWindowViewModel();
            var presenter = new LogToolWindowPresenter(CreateServiceProvider(new StubVsService(), picker), new RecordingBridgeLogger(), new TestThreadHelper(), viewModel);

            viewModel.BrowseProposalFileCommand.Execute(null);
            viewModel.ProposalProposedText = "after";
            presenter.ShowApprovalPrompt("Pending proposal", "before", "after", null, () => { }, () => { });
            presenter.CompleteProposalCycle("Apply succeeded for 'sample.cs'.");

            Assert.True(viewModel.HasLastCompletedProposalPreview);

            viewModel.ResetProposalCommand.Execute(null);

            Assert.False(viewModel.IsReviewFocusedLayoutActive);
            Assert.True(viewModel.ShowProposalEditor);
            Assert.True(viewModel.ShowSubmitProposalButton);
            Assert.False(viewModel.HasLastCompletedProposalPreview);
            Assert.False(viewModel.HasLastCompletedProposalRangePreview);
            Assert.Equal(string.Empty, viewModel.StatusMessage);
            Assert.Single(viewModel.ProposalSelectedFiles);
            Assert.Equal(path, viewModel.ProposalFilePath);
            Assert.True(viewModel.IsProposalFileLoaded);
            Assert.False(viewModel.ResetProposalCommand.CanExecute(null));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ResetProposalCommand_is_enabled_when_completed_proposal_preview_is_visible()
    {
        var path = Path.GetTempFileName();

        try
        {
            File.WriteAllText(path, "before");
            var picker = new StubProposalFilePicker { SelectedPath = path };
            var viewModel = new LogToolWindowViewModel();
            var presenter = new LogToolWindowPresenter(CreateServiceProvider(new StubVsService(), picker), new RecordingBridgeLogger(), new TestThreadHelper(), viewModel);

            viewModel.BrowseProposalFileCommand.Execute(null);
            viewModel.ProposalProposedText = "after";
            presenter.ShowApprovalPrompt("Pending proposal", "before", "after", null, () => { }, () => { });
            presenter.CompleteProposalCycle("Apply succeeded for 'sample.cs'.");

            Assert.True(viewModel.HasLastCompletedProposalPreview);
            Assert.True(viewModel.HasResettableProposalState);
            Assert.True(viewModel.ResetProposalCommand.CanExecute(null));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ResetProposalCommand_is_disabled_only_in_true_clean_state()
    {
        var viewModel = new LogToolWindowViewModel();
        _ = new LogToolWindowPresenter(CreateServiceProvider(new StubVsService()), new RecordingBridgeLogger(), new TestThreadHelper(), viewModel);

        Assert.Empty(viewModel.ProposalSelectedFiles);
        Assert.False(viewModel.HasProposalDrafts);
        Assert.False(viewModel.HasPendingApproval);
        Assert.False(viewModel.HasLastCompletedProposalPreview);
        Assert.False(viewModel.HasResettableProposalState);
        Assert.False(viewModel.HasSessionState);
        Assert.False(viewModel.ResetProposalCommand.CanExecute(null));
        Assert.False(viewModel.NewChatCommand.CanExecute(null));
    }

    [Fact]
    public void SubmitProposalCommand_submits_entered_values_through_vs_service()
    {
        var viewModel = new LogToolWindowViewModel();
        var vsService = new StubVsService();
        _ = new LogToolWindowPresenter(CreateServiceProvider(vsService), new RecordingBridgeLogger(), new TestThreadHelper(), viewModel);

        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "before");
            viewModel.ProposalFilePath = path;
            viewModel.ProposalProposedText = "after";
            viewModel.RequestInputText = "Update the file.";

            Assert.True(viewModel.SubmitProposalCommand.CanExecute(null));

            viewModel.SubmitProposalCommand.Execute(null);

            Assert.Equal(1, vsService.ProposeTextEditCalls);
            Assert.Equal(0, vsService.ProposeTextEditsCalls);
            Assert.Equal(path, viewModel.ProposalFilePath);
            Assert.NotNull(vsService.LastProposeRequestId);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void SubmitProposalCommand_submits_multiple_selected_files_through_vs_service()
    {
        var firstPath = Path.GetTempFileName();
        var secondPath = Path.GetTempFileName();

        try
        {
            File.WriteAllText(firstPath, "first-before");
            File.WriteAllText(secondPath, "second-before");

            var picker = new StubProposalFilePicker { SelectedPath = firstPath };
            var viewModel = new LogToolWindowViewModel();
            var vsService = new StubVsService();
            _ = new LogToolWindowPresenter(CreateServiceProvider(vsService, picker), new RecordingBridgeLogger(), new TestThreadHelper(), viewModel);

            viewModel.BrowseProposalFileCommand.Execute(null);
            viewModel.ProposalProposedText = "first-after";
            viewModel.RequestInputText = "Update the selected files.";

            picker.SelectedPath = secondPath;
            viewModel.BrowseProposalFileCommand.Execute(null);
            viewModel.ProposalProposedText = "second-after";

            Assert.True(viewModel.SubmitProposalCommand.CanExecute(null));

            viewModel.SubmitProposalCommand.Execute(null);

            Assert.Equal(0, vsService.ProposeTextEditCalls);
            Assert.Equal(1, vsService.ProposeTextEditsCalls);
            Assert.Equal(2, vsService.LastMultiFileEdits.Count);
            Assert.Equal(firstPath, vsService.LastMultiFileEdits[0].FilePath);
            Assert.Equal("first-after", vsService.LastMultiFileEdits[0].ProposedText);
            Assert.Equal(secondPath, vsService.LastMultiFileEdits[1].FilePath);
            Assert.Equal("second-after", vsService.LastMultiFileEdits[1].ProposedText);
        }
        finally
        {
            File.Delete(firstPath);
            File.Delete(secondPath);
        }
    }

    [Fact]
    public void ShowApprovalPrompt_populates_pending_included_files_for_multi_file_proposal()
    {
        var firstPath = Path.GetTempFileName();
        var secondPath = Path.GetTempFileName();

        try
        {
            File.WriteAllText(firstPath, "first-before");
            File.WriteAllText(secondPath, "second-before");

            var picker = new StubProposalFilePicker { SelectedPath = firstPath };
            var viewModel = new LogToolWindowViewModel();
            var presenter = new LogToolWindowPresenter(CreateServiceProvider(new StubVsService(), picker), new RecordingBridgeLogger(), new TestThreadHelper(), viewModel);

            viewModel.BrowseProposalFileCommand.Execute(null);
            picker.SelectedPath = secondPath;
            viewModel.BrowseProposalFileCommand.Execute(null);

            presenter.ShowApprovalPrompt("Pending proposal", string.Empty, string.Empty, null, () => { }, () => { });

            Assert.True(viewModel.HasPendingApprovalIncludedFiles);
            Assert.Equal("Included Files (2)", viewModel.PendingApprovalIncludedFilesHeader);
            Assert.Equal(2, viewModel.PendingApprovalIncludedFiles.Count);
            Assert.Equal(firstPath, viewModel.PendingApprovalIncludedFiles[0]);
            Assert.Equal(secondPath, viewModel.PendingApprovalIncludedFiles[1]);
        }
        finally
        {
            File.Delete(firstPath);
            File.Delete(secondPath);
        }
    }

    [Fact]
    public void CompleteProposalCycle_preserves_included_files_for_last_completed_proposal()
    {
        var firstPath = Path.GetTempFileName();
        var secondPath = Path.GetTempFileName();

        try
        {
            File.WriteAllText(firstPath, "first-before");
            File.WriteAllText(secondPath, "second-before");

            var picker = new StubProposalFilePicker { SelectedPath = firstPath };
            var viewModel = new LogToolWindowViewModel();
            var presenter = new LogToolWindowPresenter(CreateServiceProvider(new StubVsService(), picker), new RecordingBridgeLogger(), new TestThreadHelper(), viewModel);

            viewModel.BrowseProposalFileCommand.Execute(null);
            picker.SelectedPath = secondPath;
            viewModel.BrowseProposalFileCommand.Execute(null);

            presenter.ShowApprovalPrompt("Pending proposal", string.Empty, string.Empty, null, () => { }, () => { });
            presenter.CompleteProposalCycle("Apply succeeded.");

            Assert.False(viewModel.HasPendingApprovalIncludedFiles);
            Assert.True(viewModel.HasLastCompletedProposalIncludedFiles);
            Assert.Equal("Included Files (2)", viewModel.LastCompletedProposalIncludedFilesHeader);
            Assert.Equal(2, viewModel.LastCompletedProposalIncludedFiles.Count);
            Assert.Equal(firstPath, viewModel.LastCompletedProposalIncludedFiles[0]);
            Assert.Equal(secondPath, viewModel.LastCompletedProposalIncludedFiles[1]);
        }
        finally
        {
            File.Delete(firstPath);
            File.Delete(secondPath);
        }
    }

    [Fact]
    public void ShowApprovalPrompt_keeps_single_file_included_files_behavior_coherent()
    {
        var path = Path.GetTempFileName();

        try
        {
            File.WriteAllText(path, "before");
            var picker = new StubProposalFilePicker { SelectedPath = path };
            var viewModel = new LogToolWindowViewModel();
            var presenter = new LogToolWindowPresenter(CreateServiceProvider(new StubVsService(), picker), new RecordingBridgeLogger(), new TestThreadHelper(), viewModel);

            viewModel.BrowseProposalFileCommand.Execute(null);

            presenter.ShowApprovalPrompt("Pending proposal", "before", "after", null, () => { }, () => { });

            Assert.True(viewModel.HasPendingApprovalIncludedFiles);
            Assert.Equal("Included Files (1)", viewModel.PendingApprovalIncludedFilesHeader);
            Assert.Single(viewModel.PendingApprovalIncludedFiles);
            Assert.Equal(path, viewModel.PendingApprovalIncludedFiles[0]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void SubmitProposalCommand_is_enabled_when_one_selected_file_changes_and_another_remains_already_updated()
    {
        var firstPath = Path.GetTempFileName();
        var secondPath = Path.GetTempFileName();

        try
        {
            File.WriteAllText(firstPath, "needs-apply");
            File.WriteAllText(secondPath, "already-updated");

            var picker = new StubProposalFilePicker { SelectedPath = firstPath };
            var viewModel = new LogToolWindowViewModel();
            var vsService = new StubVsService();
            _ = new LogToolWindowPresenter(CreateServiceProvider(vsService, picker), new RecordingBridgeLogger(), new TestThreadHelper(), viewModel);

            viewModel.BrowseProposalFileCommand.Execute(null);
            viewModel.ProposalProposedText = "applied";
            viewModel.RequestInputText = "Apply the selected update.";

            picker.SelectedPath = secondPath;
            viewModel.BrowseProposalFileCommand.Execute(null);

            Assert.True(viewModel.IsProposalFileLoaded);
            Assert.Equal("already-updated", viewModel.ProposalOriginalText);
            Assert.Equal("already-updated", viewModel.ProposalProposedText);
            Assert.True(viewModel.SubmitProposalCommand.CanExecute(null));

            viewModel.SubmitProposalCommand.Execute(null);

            Assert.Equal(1, vsService.ProposeTextEditsCalls);
            Assert.Equal(2, vsService.LastMultiFileEdits.Count);
            Assert.Equal("applied", vsService.LastMultiFileEdits[0].ProposedText);
            Assert.Equal("already-updated", vsService.LastMultiFileEdits[1].OriginalText);
            Assert.Equal("already-updated", vsService.LastMultiFileEdits[1].ProposedText);
        }
        finally
        {
            File.Delete(firstPath);
            File.Delete(secondPath);
        }
    }

    [Fact]
    public void SubmitProposalCommand_remains_disabled_for_true_no_op_multi_file_selection()
    {
        var firstPath = Path.GetTempFileName();
        var secondPath = Path.GetTempFileName();

        try
        {
            File.WriteAllText(firstPath, "same-one");
            File.WriteAllText(secondPath, "same-two");

            var picker = new StubProposalFilePicker { SelectedPath = firstPath };
            var viewModel = new LogToolWindowViewModel();
            _ = new LogToolWindowPresenter(CreateServiceProvider(new StubVsService(), picker), new RecordingBridgeLogger(), new TestThreadHelper(), viewModel);

            viewModel.BrowseProposalFileCommand.Execute(null);

            picker.SelectedPath = secondPath;
            viewModel.BrowseProposalFileCommand.Execute(null);

            Assert.Equal(2, viewModel.ProposalSelectedFiles.Count);
            Assert.False(viewModel.SubmitProposalCommand.CanExecute(null));
        }
        finally
        {
            File.Delete(firstPath);
            File.Delete(secondPath);
        }
    }

    [Fact]
    public void SubmitProposalCommand_is_disabled_while_a_proposal_is_pending()
    {
        var viewModel = new LogToolWindowViewModel();
        var presenter = new LogToolWindowPresenter(CreateServiceProvider(new StubVsService()), new RecordingBridgeLogger(), new TestThreadHelper(), viewModel);

        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "before");
            viewModel.ProposalFilePath = path;
            viewModel.ProposalProposedText = "after";
            viewModel.RequestInputText = "Update the file.";

            Assert.True(viewModel.SubmitProposalCommand.CanExecute(null));

        presenter.ShowApprovalPrompt("Pending proposal", null, null, null, () => { }, () => { });

            Assert.False(viewModel.SubmitProposalCommand.CanExecute(null));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void SubmitProposalCommand_logs_error_when_vs_service_throws()
    {
        var logger = new RecordingBridgeLogger();
        var viewModel = new LogToolWindowViewModel();
        _ = new LogToolWindowPresenter(CreateServiceProvider(new ThrowingVsService()), logger, new TestThreadHelper(), viewModel);

        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "before");
            viewModel.ProposalFilePath = path;
            viewModel.ProposalProposedText = "after";

            viewModel.SubmitProposalCommand.Execute(null);

            var error = Assert.Single(logger.Errors);
            Assert.Contains("Manual proposal submission failed for", error.Message);
            Assert.Contains(path, error.Message);
            Assert.IsType<InvalidOperationException>(error.Exception);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ShowStatusMessage_updates_status_text_without_changing_pending_approval_state()
    {
        var viewModel = new LogToolWindowViewModel();
        var presenter = new LogToolWindowPresenter(CreateServiceProvider(new StubVsService()), new RecordingBridgeLogger(), new TestThreadHelper(), viewModel);

        presenter.ShowApprovalPrompt("Pending proposal", null, null, null, () => { }, () => { });
        presenter.ShowStatusMessage("Apply failed for 'Sample.cs'. Review the bridge log for details.");

        Assert.True(viewModel.HasPendingApproval);
        Assert.Equal("Pending proposal", viewModel.PendingApprovalDescription);
        Assert.Equal("Apply failed for 'Sample.cs'. Review the bridge log for details.", viewModel.StatusMessage);
    }

    [Fact]
    public void CompleteProposalCycle_clears_pending_state_reloads_file_and_preserves_status_message()
    {
        var viewModel = new LogToolWindowViewModel();
        var presenter = new LogToolWindowPresenter(CreateServiceProvider(new StubVsService()), new RecordingBridgeLogger(), new TestThreadHelper(), viewModel);
        var path = Path.GetTempFileName();

        try
        {
            File.WriteAllText(path, "before");
            viewModel.ProposalFilePath = path;
            viewModel.ProposalProposedText = "after";
            presenter.ShowApprovalPrompt("Pending proposal", "before", "after", null, () => { }, () => { });
            File.WriteAllText(path, "after");

            presenter.CompleteProposalCycle("Apply succeeded for 'sample.cs'.");

            Assert.False(viewModel.HasPendingApproval);
            Assert.Equal(string.Empty, viewModel.PendingApprovalDescription);
            Assert.Equal(string.Empty, viewModel.PendingApprovalOriginalSegment);
            Assert.Equal(string.Empty, viewModel.PendingApprovalUpdatedSegment);
            Assert.False(viewModel.HasPendingApprovalRangePreview);
            Assert.True(viewModel.IsProposalFileLoaded);
            Assert.Equal("after", viewModel.ProposalOriginalText);
            Assert.Equal("after", viewModel.ProposalProposedText);
            Assert.False(viewModel.IsProposalProposedTextReadOnly);
            Assert.False(viewModel.SubmitProposalCommand.CanExecute(null));
            Assert.Equal("Apply succeeded for 'sample.cs'.", viewModel.StatusMessage);
            Assert.True(viewModel.HasLastCompletedProposalPreview);
            Assert.True(viewModel.HasLastCompletedProposalRangePreview);
            Assert.Equal("before", viewModel.LastCompletedProposalOriginalText);
            Assert.Equal("after", viewModel.LastCompletedProposalUpdatedText);
            Assert.Equal("before", viewModel.LastCompletedProposalOriginalSegment);
            Assert.Equal("after", viewModel.LastCompletedProposalUpdatedSegment);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void CompletedProposal_callbacks_cannot_be_reused_accidentally()
    {
        var viewModel = new LogToolWindowViewModel();
        var presenter = new LogToolWindowPresenter(CreateServiceProvider(new StubVsService()), new RecordingBridgeLogger(), new TestThreadHelper(), viewModel);
        var approveCalls = 0;
        var rejectCalls = 0;

        presenter.ShowApprovalPrompt("Pending proposal", "before", "after", null, () => approveCalls++, () => rejectCalls++);

        viewModel.ApproveCommand.Execute(null);
        viewModel.ApproveCommand.Execute(null);
        viewModel.RejectCommand.Execute(null);

        Assert.Equal(1, approveCalls);
        Assert.Equal(0, rejectCalls);
        Assert.True(viewModel.HasPendingApproval);

        presenter.CompleteProposalCycle("Apply succeeded for 'sample.cs'.");

        viewModel.ApproveCommand.Execute(null);
        viewModel.RejectCommand.Execute(null);

        Assert.Equal(1, approveCalls);
        Assert.Equal(0, rejectCalls);
    }

    [Fact]
    public void ShowApprovalPrompt_with_range_segments_populates_focused_change_preview()
    {
        var viewModel = new LogToolWindowViewModel();
        var presenter = new LogToolWindowPresenter(CreateServiceProvider(new StubVsService()), new RecordingBridgeLogger(), new TestThreadHelper(), viewModel);

        presenter.ShowApprovalPrompt("Pending proposal", "old line", "new line", null, () => { }, () => { });

        Assert.True(viewModel.HasPendingApprovalRangePreview);
        Assert.Equal("old line", viewModel.PendingApprovalOriginalSegment);
        Assert.Equal("new line", viewModel.PendingApprovalUpdatedSegment);
    }

    [Fact]
    public void ShowApprovalPrompt_without_range_segments_keeps_focused_change_preview_hidden()
    {
        var viewModel = new LogToolWindowViewModel();
        var presenter = new LogToolWindowPresenter(CreateServiceProvider(new StubVsService()), new RecordingBridgeLogger(), new TestThreadHelper(), viewModel);

        presenter.ShowApprovalPrompt("Pending proposal", string.Empty, string.Empty, null, () => { }, () => { });

        Assert.False(viewModel.HasPendingApprovalRangePreview);
        Assert.Equal(string.Empty, viewModel.PendingApprovalOriginalSegment);
        Assert.Equal(string.Empty, viewModel.PendingApprovalUpdatedSegment);
    }

    [Fact]
    public void ShowApprovalPrompt_with_multi_range_reviewed_changes_populates_pending_change_list()
    {
        var viewModel = new LogToolWindowViewModel();
        var presenter = new LogToolWindowPresenter(CreateServiceProvider(new StubVsService()), new RecordingBridgeLogger(), new TestThreadHelper(), viewModel);
        var reviewedChanges = new List<ProposalReviewedChange>
        {
            new() { SequenceNumber = 1, OriginalSegment = "\"pending\"", UpdatedSegment = "\"approved\"" },
            new() { SequenceNumber = 2, OriginalSegment = "\"pending\"", UpdatedSegment = "\"archived\"" }
        };

        presenter.ShowApprovalPrompt("Pending proposal", string.Empty, string.Empty, reviewedChanges, () => { }, () => { });

        Assert.True(viewModel.HasPendingApprovalReviewedChanges);
        Assert.Equal(2, viewModel.PendingApprovalReviewedChanges.Count);
        Assert.Equal(1, viewModel.PendingApprovalReviewedChanges[0].SequenceNumber);
        Assert.Equal("\"pending\"", viewModel.PendingApprovalReviewedChanges[0].OriginalSegment);
        Assert.Equal("\"archived\"", viewModel.PendingApprovalReviewedChanges[1].UpdatedSegment);
    }

    [Fact]
    public void ShowApprovalPrompt_with_single_range_preview_keeps_reviewed_change_list_empty()
    {
        var viewModel = new LogToolWindowViewModel();
        var presenter = new LogToolWindowPresenter(CreateServiceProvider(new StubVsService()), new RecordingBridgeLogger(), new TestThreadHelper(), viewModel);

        presenter.ShowApprovalPrompt("Pending proposal", "before", "after", null, () => { }, () => { });

        Assert.False(viewModel.HasPendingApprovalReviewedChanges);
        Assert.Empty(viewModel.PendingApprovalReviewedChanges);
        Assert.True(viewModel.HasPendingApprovalRangePreview);
    }

    [Fact]
    public void ShowApprovalPrompt_uses_explicit_included_files_instead_of_editor_selection()
    {
        var viewModel = new LogToolWindowViewModel
        {
            ProposalSelectedFiles = new[] { "stale.cs" }
        };
        var presenter = new LogToolWindowPresenter(CreateServiceProvider(new StubVsService()), new RecordingBridgeLogger(), new TestThreadHelper(), viewModel);

        presenter.ShowApprovalPrompt(
            "Pending proposal",
            null,
            null,
            null,
            () => { },
            () => { },
            new[] { "first.cs", "second.cs" });

        Assert.True(viewModel.HasPendingApprovalIncludedFiles);
        Assert.Equal(2, viewModel.PendingApprovalIncludedFiles.Count);
        Assert.Equal("first.cs", viewModel.PendingApprovalIncludedFiles[0]);
        Assert.Equal("second.cs", viewModel.PendingApprovalIncludedFiles[1]);
    }

    [Fact]
    public void CompleteProposalCycle_preserves_multi_range_reviewed_changes_for_last_completed_proposal()
    {
        var viewModel = new LogToolWindowViewModel();
        var presenter = new LogToolWindowPresenter(CreateServiceProvider(new StubVsService()), new RecordingBridgeLogger(), new TestThreadHelper(), viewModel);
        var path = Path.GetTempFileName();
        var reviewedChanges = new List<ProposalReviewedChange>
        {
            new() { SequenceNumber = 1, OriginalSegment = "\"pending\"", UpdatedSegment = "\"approved\"" },
            new() { SequenceNumber = 2, OriginalSegment = "\"pending\"", UpdatedSegment = "\"archived\"" }
        };

        try
        {
            File.WriteAllText(path, "after");
            viewModel.ProposalFilePath = path;
            viewModel.ProposalOriginalText = "before";
            viewModel.ProposalProposedText = "after";

            presenter.ShowApprovalPrompt("Pending proposal", string.Empty, string.Empty, reviewedChanges, () => { }, () => { });
            presenter.CompleteProposalCycle("Apply succeeded for 'sample.cs'.");

            Assert.False(viewModel.HasPendingApprovalReviewedChanges);
            Assert.Empty(viewModel.PendingApprovalReviewedChanges);
            Assert.True(viewModel.HasLastCompletedProposalReviewedChanges);
            Assert.Equal(2, viewModel.LastCompletedProposalReviewedChanges.Count);
            Assert.Equal(1, viewModel.LastCompletedProposalReviewedChanges[0].SequenceNumber);
            Assert.Equal("\"approved\"", viewModel.LastCompletedProposalReviewedChanges[0].UpdatedSegment);
            Assert.Equal("\"archived\"", viewModel.LastCompletedProposalReviewedChanges[1].UpdatedSegment);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ShowApprovalPrompt_clears_last_completed_proposal_preview_when_new_review_starts()
    {
        var viewModel = new LogToolWindowViewModel();
        var presenter = new LogToolWindowPresenter(CreateServiceProvider(new StubVsService()), new RecordingBridgeLogger(), new TestThreadHelper(), viewModel);

        viewModel.LastCompletedProposalOriginalText = "before";
        viewModel.LastCompletedProposalUpdatedText = "after";
        viewModel.LastCompletedProposalOriginalSegment = "old line";
        viewModel.LastCompletedProposalUpdatedSegment = "new line";

        presenter.ShowApprovalPrompt("Pending proposal", "segment before", "segment after", null, () => { }, () => { });

        Assert.False(viewModel.HasLastCompletedProposalPreview);
        Assert.False(viewModel.HasLastCompletedProposalRangePreview);
        Assert.Equal(string.Empty, viewModel.LastCompletedProposalOriginalText);
        Assert.Equal(string.Empty, viewModel.LastCompletedProposalUpdatedText);
        Assert.Equal(string.Empty, viewModel.LastCompletedProposalOriginalSegment);
        Assert.Equal(string.Empty, viewModel.LastCompletedProposalUpdatedSegment);
        Assert.True(viewModel.IsReviewFocusedLayoutActive);
        Assert.True(viewModel.ShowProposalEditor);
        Assert.False(viewModel.ShowSubmitProposalButton);
    }

    [Fact]
    public void LastCompletedProposalPreview_keeps_editor_visible_but_hides_submit_until_a_new_proposal_is_started()
    {
        var viewModel = new LogToolWindowViewModel();

        Assert.False(viewModel.IsReviewFocusedLayoutActive);
        Assert.True(viewModel.ShowProposalEditor);
        Assert.True(viewModel.ShowSubmitProposalButton);

        viewModel.LastCompletedProposalOriginalText = "before";
        viewModel.LastCompletedProposalUpdatedText = "after";

        Assert.True(viewModel.IsReviewFocusedLayoutActive);
        Assert.True(viewModel.HasLastCompletedProposalPreview);
        Assert.True(viewModel.ShowProposalEditor);
        Assert.False(viewModel.ShowSubmitProposalButton);

        viewModel.LastCompletedProposalOriginalText = string.Empty;
        viewModel.LastCompletedProposalUpdatedText = string.Empty;

        Assert.False(viewModel.IsReviewFocusedLayoutActive);
        Assert.False(viewModel.HasLastCompletedProposalPreview);
        Assert.True(viewModel.ShowProposalEditor);
        Assert.True(viewModel.ShowSubmitProposalButton);
    }

    [Fact]
    public void PendingReview_activates_review_focused_layout_and_hides_submit_action()
    {
        var viewModel = new LogToolWindowViewModel();
        var presenter = new LogToolWindowPresenter(CreateServiceProvider(new StubVsService()), new RecordingBridgeLogger(), new TestThreadHelper(), viewModel);

        presenter.ShowApprovalPrompt("Pending proposal", "before", "after", null, () => { }, () => { });

        Assert.True(viewModel.HasPendingApproval);
        Assert.True(viewModel.IsReviewFocusedLayoutActive);
        Assert.True(viewModel.ShowProposalEditor);
        Assert.False(viewModel.ShowSubmitProposalButton);
    }

    [Fact]
    public void RequestSummaries_reflect_selected_file_count_and_active_file()
    {
        var viewModel = new LogToolWindowViewModel
        {
            ProposalSelectedFiles = new[] { @"C:\repo\First.cs", @"C:\repo\Second.cs" },
            ProposalFilePath = @"C:\repo\Second.cs"
        };

        Assert.Equal("2 files selected for this request.", viewModel.ProposalSelectionSummary);
        Assert.Equal(@"Current file: C:\repo\Second.cs", viewModel.ProposalActiveFileSummary);
    }

    [Fact]
    public void RequestPhaseSummary_defaults_to_request_entry_guidance()
    {
        var viewModel = new LogToolWindowViewModel();

        Assert.True(viewModel.HasRequestPhaseSummary);
        Assert.Equal(
            "Select one or more files, prepare the proposed content, and submit when the request is ready for review.",
            viewModel.RequestPhaseSummary);
    }

    [Fact]
    public void RequestPhaseSummary_tracks_review_and_completed_states()
    {
        var viewModel = new LogToolWindowViewModel();
        var presenter = new LogToolWindowPresenter(CreateServiceProvider(new StubVsService()), new RecordingBridgeLogger(), new TestThreadHelper(), viewModel);

        viewModel.ProposalSelectedFiles = new[] { "sample.cs" };
        Assert.Equal(
            "Adjust the proposed content if needed, then submit when at least one selected file has a meaningful change.",
            viewModel.RequestPhaseSummary);

        presenter.ShowApprovalPrompt("Pending proposal", "before", "after", null, () => { }, () => { });
        Assert.Equal(
            "Approval review is ready. Inspect the concise review below, then choose Keep or Reject.",
            viewModel.RequestPhaseSummary);

        viewModel.HasPendingApproval = false;
        viewModel.LastCompletedProposalOriginalText = "before";
        viewModel.LastCompletedProposalUpdatedText = "after";
        Assert.Equal(
            "Last proposal result is available below. Review the summary or reset to start the next request.",
            viewModel.RequestPhaseSummary);
    }

    [Fact]
    public void SubmitProposalCommand_captures_request_echo_and_marks_activity_in_progress()
    {
        var path = Path.GetTempFileName();

        try
        {
            File.WriteAllText(path, "before");
            var picker = new StubProposalFilePicker { SelectedPath = path };
            var viewModel = new LogToolWindowViewModel();
            var vsService = new StubVsService();
            _ = new LogToolWindowPresenter(CreateServiceProvider(vsService, picker), new RecordingBridgeLogger(), new TestThreadHelper(), viewModel);

            viewModel.BrowseProposalFileCommand.Execute(null);
            viewModel.ProposalProposedText = "after";
            viewModel.RequestInputText = "Update the selected file to the approved version.";

            viewModel.SubmitProposalCommand.Execute(null);

            Assert.Equal("Update the selected file to the approved version.", viewModel.LastSubmittedRequestText);
            Assert.True(viewModel.HasLastSubmittedRequest);
            Assert.True(viewModel.IsRequestInProgress);
            Assert.Equal("Working", viewModel.ActivityTitle);
            Assert.Equal(1, vsService.ProposeTextEditCalls);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void SubmitProposalCommand_requires_meaningful_request_text()
    {
        var path = Path.GetTempFileName();

        try
        {
            File.WriteAllText(path, "before");
            var viewModel = new LogToolWindowViewModel();
            _ = new LogToolWindowPresenter(CreateServiceProvider(new StubVsService()), new RecordingBridgeLogger(), new TestThreadHelper(), viewModel);

            viewModel.ProposalFilePath = path;
            viewModel.ProposalProposedText = "after";

            Assert.False(viewModel.SubmitProposalCommand.CanExecute(null));

            viewModel.RequestInputText = "   ";
            Assert.False(viewModel.SubmitProposalCommand.CanExecute(null));

            viewModel.RequestInputText = "Update the file.";
            Assert.True(viewModel.SubmitProposalCommand.CanExecute(null));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ShowApprovalPrompt_clears_in_progress_state_and_switches_to_review_activity()
    {
        var viewModel = new LogToolWindowViewModel { IsRequestInProgress = true };
        var presenter = new LogToolWindowPresenter(CreateServiceProvider(new StubVsService()), new RecordingBridgeLogger(), new TestThreadHelper(), viewModel);

        presenter.ShowApprovalPrompt("Pending proposal", "before", "after", null, () => { }, () => { }, new[] { "sample.cs" });

        Assert.False(viewModel.IsRequestInProgress);
        Assert.True(viewModel.HasPendingApproval);
        Assert.Equal("Review Ready", viewModel.ActivityTitle);
        Assert.Equal("1 file affected", viewModel.ActivityMetricsSummary);
    }

    [Fact]
    public void ResetProposalCommand_clears_request_input_and_request_echo()
    {
        var path = Path.GetTempFileName();

        try
        {
            File.WriteAllText(path, "before");
            var picker = new StubProposalFilePicker { SelectedPath = path };
            var viewModel = new LogToolWindowViewModel();
            _ = new LogToolWindowPresenter(CreateServiceProvider(new StubVsService(), picker), new RecordingBridgeLogger(), new TestThreadHelper(), viewModel);

            viewModel.BrowseProposalFileCommand.Execute(null);
            viewModel.ProposalProposedText = "after";
            viewModel.RequestInputText = "Prepare the draft update.";
            viewModel.LastSubmittedRequestText = "Prepare the draft update.";
            viewModel.IsRequestInProgress = true;

            viewModel.ResetProposalCommand.Execute(null);

            Assert.Equal(string.Empty, viewModel.RequestInputText);
            Assert.Equal(string.Empty, viewModel.LastSubmittedRequestText);
            Assert.False(viewModel.HasLastSubmittedRequest);
            Assert.False(viewModel.IsRequestInProgress);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ResetProposalCommand_is_enabled_while_request_is_working()
    {
        var viewModel = new LogToolWindowViewModel
        {
            IsRequestInProgress = true
        };
        _ = new LogToolWindowPresenter(CreateServiceProvider(new StubVsService()), new RecordingBridgeLogger(), new TestThreadHelper(), viewModel);

        Assert.True(viewModel.HasResettableProposalState);
        Assert.True(viewModel.ResetProposalCommand.CanExecute(null));
    }

    [Fact]
    public void ShowApprovalPrompt_ignores_suppressed_request_after_reset_during_working()
    {
        var viewModel = new LogToolWindowViewModel
        {
            IsRequestInProgress = true,
            RequestInputText = "Update the file.",
            LastSubmittedRequestText = "Update the file."
        };
        var presenter = new LogToolWindowPresenter(CreateServiceProvider(new StubVsService()), new RecordingBridgeLogger(), new TestThreadHelper(), viewModel);
        var presenterType = typeof(LogToolWindowPresenter);

        presenterType.GetField("_activeManualRequestId", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(presenter, "request-1");

        viewModel.ResetProposalCommand.Execute(null);
        presenter.ShowApprovalPrompt("Pending proposal", "before", "after", null, () => { }, () => { }, new[] { "sample.cs" }, "request-1");

        Assert.False(viewModel.HasPendingApproval);
        Assert.False(viewModel.IsRequestInProgress);
        Assert.Equal(string.Empty, viewModel.LastSubmittedRequestText);
        Assert.Equal(string.Empty, viewModel.RequestInputText);
    }

    [Fact]
    public void OpenGitChangesCommand_is_enabled_for_completed_result_and_invokes_vs_service()
    {
        var path = Path.GetTempFileName();

        try
        {
            File.WriteAllText(path, "after");
            var picker = new StubProposalFilePicker { SelectedPath = path };
            var viewModel = new LogToolWindowViewModel();
            var vsService = new StubVsService();
            var presenter = new LogToolWindowPresenter(CreateServiceProvider(vsService, picker), new RecordingBridgeLogger(), new TestThreadHelper(), viewModel);

            viewModel.BrowseProposalFileCommand.Execute(null);
            viewModel.ProposalOriginalText = "before";
            viewModel.ProposalProposedText = "after";
            presenter.ShowApprovalPrompt("Pending proposal", "before", "after", null, () => { }, () => { }, new[] { path });
            presenter.CompleteProposalCycle("Apply succeeded for 1 file. All approved changes were applied.");

            Assert.True(viewModel.CanOpenGitChanges);
            Assert.True(viewModel.OpenGitChangesCommand.CanExecute(null));

            viewModel.OpenGitChangesCommand.Execute(null);

            Assert.Equal(1, vsService.OpenGitChangesCalls);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Reset_after_pending_review_restores_authoring_layout()
    {
        var path = Path.GetTempFileName();

        try
        {
            File.WriteAllText(path, "before");
            var picker = new StubProposalFilePicker { SelectedPath = path };
            var viewModel = new LogToolWindowViewModel();
            var presenter = new LogToolWindowPresenter(CreateServiceProvider(new StubVsService(), picker), new RecordingBridgeLogger(), new TestThreadHelper(), viewModel);

            viewModel.BrowseProposalFileCommand.Execute(null);
            presenter.ShowApprovalPrompt("Pending proposal", "before", "after", null, () => { }, () => { });

            Assert.True(viewModel.IsReviewFocusedLayoutActive);
            Assert.True(viewModel.ShowProposalEditor);
            Assert.False(viewModel.ShowSubmitProposalButton);

            viewModel.ResetProposalCommand.Execute(null);

            Assert.False(viewModel.HasPendingApproval);
            Assert.False(viewModel.IsReviewFocusedLayoutActive);
            Assert.True(viewModel.ShowProposalEditor);
            Assert.True(viewModel.ShowSubmitProposalButton);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ProposalFilePath_loads_file_contents_into_both_panes_and_disables_submit_until_text_changes()
    {
        var viewModel = new LogToolWindowViewModel();
        _ = new LogToolWindowPresenter(CreateServiceProvider(new StubVsService()), new RecordingBridgeLogger(), new TestThreadHelper(), viewModel);
        var path = Path.GetTempFileName();

        try
        {
            File.WriteAllText(path, "before\r\nafter\r\n");

            viewModel.ProposalFilePath = path;

            Assert.True(viewModel.IsProposalFileLoaded);
            Assert.Single(viewModel.ProposalSelectedFiles);
            Assert.Equal("before\r\nafter\r\n", viewModel.ProposalOriginalText);
            Assert.Equal("before\r\nafter\r\n", viewModel.ProposalProposedText);
            Assert.True(viewModel.IsProposalOriginalTextReadOnly);
            Assert.False(viewModel.IsProposalProposedTextReadOnly);
            Assert.False(viewModel.SubmitProposalCommand.CanExecute(null));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void SubmitProposalCommand_enablement_tracks_proposed_text_equality_against_loaded_original()
    {
        var viewModel = new LogToolWindowViewModel();
        _ = new LogToolWindowPresenter(CreateServiceProvider(new StubVsService()), new RecordingBridgeLogger(), new TestThreadHelper(), viewModel);
        var path = Path.GetTempFileName();

        try
        {
            File.WriteAllText(path, "before");
            viewModel.ProposalFilePath = path;

            Assert.False(viewModel.SubmitProposalCommand.CanExecute(null));

            viewModel.ProposalProposedText = "after";
            viewModel.RequestInputText = "Update the file.";
            Assert.True(viewModel.SubmitProposalCommand.CanExecute(null));

            viewModel.ProposalProposedText = "before";
            Assert.True(viewModel.SubmitProposalCommand.CanExecute(null));

            viewModel.RequestInputText = string.Empty;
            Assert.False(viewModel.SubmitProposalCommand.CanExecute(null));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ProposalFilePath_load_failure_clears_panes_disables_submit_and_surfaces_status()
    {
        var viewModel = new LogToolWindowViewModel();
        _ = new LogToolWindowPresenter(CreateServiceProvider(new StubVsService()), new RecordingBridgeLogger(), new TestThreadHelper(), viewModel);
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.txt");

        viewModel.ProposalFilePath = path;

        Assert.False(viewModel.IsProposalFileLoaded);
        Assert.Equal(string.Empty, viewModel.ProposalOriginalText);
        Assert.Equal(string.Empty, viewModel.ProposalProposedText);
        Assert.False(viewModel.SubmitProposalCommand.CanExecute(null));
        Assert.Equal($"Unable to load file '{path}'.", viewModel.StatusMessage);
    }

    [Fact]
    public void BrowseProposalFileCommand_selection_uses_existing_load_flow_for_pane_population_and_submit_gating()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "before");
            var picker = new StubProposalFilePicker { SelectedPath = path };
            var viewModel = new LogToolWindowViewModel();
            _ = new LogToolWindowPresenter(CreateServiceProvider(new StubVsService(), picker), new RecordingBridgeLogger(), new TestThreadHelper(), viewModel);

            viewModel.BrowseProposalFileCommand.Execute(null);

            Assert.Equal(path, viewModel.ProposalFilePath);
            Assert.True(viewModel.IsProposalFileLoaded);
            Assert.Equal("before", viewModel.ProposalOriginalText);
            Assert.Equal("before", viewModel.ProposalProposedText);
            Assert.False(viewModel.SubmitProposalCommand.CanExecute(null));

            viewModel.ProposalProposedText = "after";
            viewModel.RequestInputText = "Update the file.";

            Assert.True(viewModel.SubmitProposalCommand.CanExecute(null));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Changing_proposal_file_path_clears_last_completed_preview_and_reloads_editor_state()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "before");
            var viewModel = new LogToolWindowViewModel();
            _ = new LogToolWindowPresenter(CreateServiceProvider(new StubVsService()), new RecordingBridgeLogger(), new TestThreadHelper(), viewModel);

            viewModel.LastCompletedProposalOriginalText = "old before";
            viewModel.LastCompletedProposalUpdatedText = "old after";
            viewModel.LastCompletedProposalOriginalSegment = "old segment";
            viewModel.LastCompletedProposalUpdatedSegment = "new segment";

            viewModel.ProposalFilePath = path;

            Assert.False(viewModel.HasLastCompletedProposalPreview);
            Assert.False(viewModel.HasLastCompletedProposalRangePreview);
            Assert.True(viewModel.ShowProposalEditor);
            Assert.Equal("before", viewModel.ProposalOriginalText);
            Assert.Equal("before", viewModel.ProposalProposedText);
        }
        finally
        {
            File.Delete(path);
        }
    }

}
