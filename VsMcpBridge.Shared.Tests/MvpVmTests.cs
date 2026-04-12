using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using VsMcpBridge.Shared.Composition;
using VsMcpBridge.Shared.Interfaces;
using VsMcpBridge.Shared.Loggers;
using VsMcpBridge.Shared.MvpVm;
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

        presenter.ShowApprovalPrompt("Apply change", () => approved = true, () => rejected = true);

        Assert.True(viewModel.HasPendingApproval);
        Assert.Equal("Apply change", viewModel.PendingApprovalDescription);
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

        presenter.ShowApprovalPrompt("Pending proposal", () => { }, () => { });

        Assert.True(viewModel.IsProposalOriginalTextReadOnly);
        Assert.True(viewModel.IsProposalProposedTextReadOnly);
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
        presenter.ShowApprovalPrompt("Pending proposal", () => approved = true, () => { });

        viewModel.ProposalFilePath = @"C:\repo\Different.cs";
        viewModel.ProposalOriginalText = "edited in proposal pane";
        viewModel.ProposalProposedText = "different proposed text";

        viewModel.ApproveCommand.Execute(null);

        Assert.True(approved);
        Assert.True(viewModel.HasPendingApproval);
        Assert.Equal("Pending proposal", viewModel.PendingApprovalDescription);
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

        presenter.ShowApprovalPrompt("Pending proposal", () => { }, () => { });

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

            Assert.True(viewModel.SubmitProposalCommand.CanExecute(null));

            viewModel.SubmitProposalCommand.Execute(null);

            Assert.Equal(1, vsService.ProposeTextEditCalls);
            Assert.Equal(path, viewModel.ProposalFilePath);
            Assert.NotNull(vsService.LastProposeRequestId);
        }
        finally
        {
            File.Delete(path);
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

            Assert.True(viewModel.SubmitProposalCommand.CanExecute(null));

            presenter.ShowApprovalPrompt("Pending proposal", () => { }, () => { });

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
            Assert.Contains($"Manual proposal submission failed for '{path}'.", error.Message);
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

        presenter.ShowApprovalPrompt("Pending proposal", () => { }, () => { });
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
            presenter.ShowApprovalPrompt("Pending proposal", () => { }, () => { });

            presenter.CompleteProposalCycle("Apply succeeded for 'sample.cs'.");

            Assert.False(viewModel.HasPendingApproval);
            Assert.Equal(string.Empty, viewModel.PendingApprovalDescription);
            Assert.True(viewModel.IsProposalFileLoaded);
            Assert.Equal("before", viewModel.ProposalOriginalText);
            Assert.Equal("before", viewModel.ProposalProposedText);
            Assert.False(viewModel.IsProposalProposedTextReadOnly);
            Assert.False(viewModel.SubmitProposalCommand.CanExecute(null));
            Assert.Equal("Apply succeeded for 'sample.cs'.", viewModel.StatusMessage);
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

        presenter.ShowApprovalPrompt("Pending proposal", () => approveCalls++, () => rejectCalls++);

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
            Assert.True(viewModel.SubmitProposalCommand.CanExecute(null));

            viewModel.ProposalProposedText = "before";
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

            Assert.True(viewModel.SubmitProposalCommand.CanExecute(null));
        }
        finally
        {
            File.Delete(path);
        }
    }

}
