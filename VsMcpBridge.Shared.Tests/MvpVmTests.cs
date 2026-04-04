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
        var presenter = new LogToolWindowPresenter(logger, threadHelper);
        var control = new FakeLogToolWindowControl();
        var viewModel = new LogToolWindowViewModel();

        presenter.LogToolWindowControl = control;
        presenter.LogToolWindowViewModel = viewModel;

        presenter.Initialize();

        Assert.Same(viewModel, control.DataContext);
        Assert.Contains("Initializing VS MCP Bridge tool window...", logger.InformationMessages);
        Assert.Contains("VS MCP Bridge tool window Initialized.", logger.InformationMessages);
    }

    [Fact]
    public void AppendLog_replaces_initial_placeholder_and_appends_on_subsequent_calls()
    {
        var presenter = new LogToolWindowPresenter(new RecordingBridgeLogger(), new TestThreadHelper());
        var viewModel = new LogToolWindowViewModel();

        presenter.LogToolWindowControl = new FakeLogToolWindowControl();
        presenter.LogToolWindowViewModel = viewModel;

        presenter.AppendLog("first");
        presenter.AppendLog("second");

        Assert.Equal($"first{System.Environment.NewLine}second", viewModel.LogText);
    }

    [Fact]
    public void ShowApprovalPrompt_updates_view_model_and_approve_command_invokes_callback()
    {
        var presenter = new LogToolWindowPresenter(new RecordingBridgeLogger(), new TestThreadHelper());
        var control = new FakeLogToolWindowControl();
        var viewModel = new LogToolWindowViewModel();
        var approved = false;
        var rejected = false;

        presenter.LogToolWindowControl = control;
        presenter.LogToolWindowViewModel = viewModel;
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
        var presenter = new LogToolWindowPresenter(logger, threadHelper)
        {
            LogToolWindowControl = new FakeLogToolWindowControl(),
            LogToolWindowViewModel = new LogToolWindowViewModel()
        };

        presenter.AppendLog("message");

        Assert.Equal(1, threadHelper.RunCalls);
        Assert.Equal(1, threadHelper.SwitchCalls);
        Assert.Equal("message", presenter.LogToolWindowViewModel.LogText);
    }
}
