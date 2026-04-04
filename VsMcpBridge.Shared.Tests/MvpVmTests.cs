using Microsoft.Extensions.DependencyInjection;
using VsMcpBridge.Shared.Composition;
using VsMcpBridge.Shared.Interfaces;
using VsMcpBridge.Shared.MvpVm;
using VsMcpBridge.Shared.Tests.Support;
using Xunit;

namespace VsMcpBridge.Shared.Tests;

public sealed class MvpVmTests
{
    [Fact]
    public void AddMvpVmServices_registers_shared_presenter_and_view_model()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IBridgeLogger, RecordingBridgeLogger>();
        services.AddSingleton<IThreadHelper, TestThreadHelper>();

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
        var presenter = new LogToolWindowPresenter(logger, threadHelper, viewModel);
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
        var presenter = new LogToolWindowPresenter(new RecordingBridgeLogger(), new TestThreadHelper(), viewModel);

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
        var presenter = new LogToolWindowPresenter(new RecordingBridgeLogger(), new TestThreadHelper(), viewModel);
        var approved = false;
        var rejected = false;

        presenter.LogToolWindowControl = control;
        presenter.Initialize();

        presenter.ShowApprovalPrompt("Apply change", () => approved = true, () => rejected = true);

        Assert.True(viewModel.HasPendingApproval);
        Assert.Equal("Apply change", viewModel.PendingApprovalDescription);
        Assert.True(viewModel.ApproveCommand.CanExecute(null));

        viewModel.ApproveCommand.Execute(null);

        Assert.True(approved);
        Assert.False(rejected);
        Assert.False(viewModel.HasPendingApproval);
        Assert.Equal(string.Empty, viewModel.PendingApprovalDescription);
    }

    [Fact]
    public void AppendLog_switches_to_main_thread_when_access_is_not_available()
    {
        var logger = new RecordingBridgeLogger();
        var threadHelper = new TestThreadHelper { HasAccess = false };
        var viewModel = new LogToolWindowViewModel();
        var presenter = new LogToolWindowPresenter(logger, threadHelper, viewModel)
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
        var presenter = new LogToolWindowPresenter(new RecordingBridgeLogger(), new TestThreadHelper(), viewModel);

        presenter.ShowApprovalPrompt("Pending proposal", () => { }, () => { });

        Assert.True(viewModel.HasPendingApproval);
        Assert.Equal("Pending proposal", viewModel.PendingApprovalDescription);
        Assert.True(viewModel.ApproveCommand.CanExecute(null));
        Assert.True(viewModel.RejectCommand.CanExecute(null));
    }

    [Fact]
    public void SubmitProposalCommand_invokes_submission_handler_with_entered_values()
    {
        var viewModel = new LogToolWindowViewModel();
        var presenter = new LogToolWindowPresenter(new RecordingBridgeLogger(), new TestThreadHelper(), viewModel);
        string? submittedFilePath = null;
        string? submittedOriginalText = null;
        string? submittedProposedText = null;

        presenter.SetProposalSubmissionHandler((filePath, originalText, proposedText) =>
        {
            submittedFilePath = filePath;
            submittedOriginalText = originalText;
            submittedProposedText = proposedText;
        });

        viewModel.ProposalFilePath = @"C:\repo\Sample.cs";
        viewModel.ProposalOriginalText = "before";
        viewModel.ProposalProposedText = "after";

        Assert.True(viewModel.SubmitProposalCommand.CanExecute(null));

        viewModel.SubmitProposalCommand.Execute(null);

        Assert.Equal(@"C:\repo\Sample.cs", submittedFilePath);
        Assert.Equal("before", submittedOriginalText);
        Assert.Equal("after", submittedProposedText);
    }

    [Fact]
    public void SubmitProposalCommand_is_disabled_while_a_proposal_is_pending()
    {
        var viewModel = new LogToolWindowViewModel();
        var presenter = new LogToolWindowPresenter(new RecordingBridgeLogger(), new TestThreadHelper(), viewModel);

        presenter.SetProposalSubmissionHandler((_, _, _) => { });
        viewModel.ProposalFilePath = @"C:\repo\Sample.cs";
        viewModel.ProposalOriginalText = "before";
        viewModel.ProposalProposedText = "after";

        Assert.True(viewModel.SubmitProposalCommand.CanExecute(null));

        presenter.ShowApprovalPrompt("Pending proposal", () => { }, () => { });

        Assert.False(viewModel.SubmitProposalCommand.CanExecute(null));
    }
}
