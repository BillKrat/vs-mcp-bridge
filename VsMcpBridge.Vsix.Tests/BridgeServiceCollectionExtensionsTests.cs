using Microsoft.Extensions.DependencyInjection;
using VsMcpBridge.Shared.Interfaces;
using VsMcpBridge.Vsix.Composition;
using VsMcpBridge.Vsix.Diagnostics;
using VsMcpBridge.Vsix.Logging;
using VsMcpBridge.Vsix.Pipe;
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

        Assert.Same(package, provider.GetRequiredService<Microsoft.VisualStudio.Shell.AsyncPackage>());
        Assert.IsType<ActivityLogBridgeLogger>(provider.GetRequiredService<IBridgeLogger>());
        Assert.IsType<FileUnhandledExceptionSink>(provider.GetRequiredService<IUnhandledExceptionSink>());
        Assert.IsType<VsService>(provider.GetRequiredService<IVsService>());
        Assert.IsType<PipeServer>(provider.GetRequiredService<IPipeServer>());
        Assert.Same(provider.GetRequiredService<IVsService>(), provider.GetRequiredService<IVsService>());
        Assert.Same(provider.GetRequiredService<IPipeServer>(), provider.GetRequiredService<IPipeServer>());
        Assert.Same(provider.GetRequiredService<IUnhandledExceptionSink>(), provider.GetRequiredService<IUnhandledExceptionSink>());
    }
}
