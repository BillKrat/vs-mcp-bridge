using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Shell;
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
        using var provider = services.BuildServiceProvider();

        Assert.Same(package, provider.GetRequiredService<IAsyncPackage>());
        Assert.Same(package, provider.GetRequiredService<AsyncPackage>());
        Assert.IsType<ActivityLogBridgeLogger>(provider.GetRequiredService<IBridgeLogger>());
        Assert.IsType<FileUnhandledExceptionSink>(provider.GetRequiredService<IUnhandledExceptionSink>());
        Assert.NotNull(provider.GetRequiredService<IThreadHelper>());
        Assert.IsType<VsService>(provider.GetRequiredService<IVsService>());
        Assert.IsType<PipeServer>(provider.GetRequiredService<IPipeServer>());
        Assert.Same(provider.GetRequiredService<IVsService>(), provider.GetRequiredService<IVsService>());
        Assert.Same(provider.GetRequiredService<IPipeServer>(), provider.GetRequiredService<IPipeServer>());
        Assert.Same(provider.GetRequiredService<IUnhandledExceptionSink>(), provider.GetRequiredService<IUnhandledExceptionSink>());
    }
}
