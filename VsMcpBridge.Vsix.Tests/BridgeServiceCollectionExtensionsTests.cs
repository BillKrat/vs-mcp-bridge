using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Shell;
using VsMcpBridge.Shared.Composition;
using VsMcpBridge.Shared.Diagnostics;
using VsMcpBridge.Shared.Interfaces;
using VsMcpBridge.Shared.Services;
using VsMcpBridge.Vsix.Composition;
using VsMcpBridge.Vsix.Logging;
using VsMcpBridge.Vsix.Services;
using VsMcpBridge.Vsix.Tests.Support;
using Xunit;

namespace VsMcpBridge.Vsix.Tests;

public sealed class BridgeServiceCollectionExtensionsTests
{
    [Fact]
    public void AddVsMcpBridgeServices_registers_expected_singletons()
    {
        var package = TestPackageFactory.CreatePackage();
        var services = new ServiceCollection();

        services.AddVsMcpBridgeServices(package);
        services.AddMvpVmServices();
        using var provider = services.BuildServiceProvider();

        Assert.Same(package, provider.GetRequiredService<IAsyncPackage>());
        Assert.Same(package, provider.GetRequiredService<AsyncPackage>());
        Assert.IsType<ActivityLogBridgeLogger>(provider.GetRequiredService<ILogger>());
        Assert.IsType<FileUnhandledExceptionSink>(provider.GetRequiredService<IUnhandledExceptionSink>());
        Assert.IsType<InMemoryApprovalWorkflowService>(provider.GetRequiredService<IApprovalWorkflowService>());
        Assert.NotNull(provider.GetRequiredService<IEditApplier>());
        Assert.NotNull(provider.GetRequiredService<IThreadHelper>());
        Assert.IsType<VsService>(provider.GetRequiredService<IVsService>());
        Assert.IsType<PipeServer>(provider.GetRequiredService<IPipeServer>());
        Assert.Same(provider.GetRequiredService<IVsService>(), provider.GetRequiredService<IVsService>());
        Assert.Same(provider.GetRequiredService<IPipeServer>(), provider.GetRequiredService<IPipeServer>());
        Assert.Same(provider.GetRequiredService<IUnhandledExceptionSink>(), provider.GetRequiredService<IUnhandledExceptionSink>());
    }
}
