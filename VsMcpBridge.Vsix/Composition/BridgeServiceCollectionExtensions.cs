using Microsoft.Extensions.DependencyInjection;
using VsMcpBridge.Shared.Interfaces;
using VsMcpBridge.Vsix.Diagnostics;
using VsMcpBridge.Vsix.Logging;
using VsMcpBridge.Vsix.Pipe;
using VsMcpBridge.Vsix.Services;

namespace VsMcpBridge.Vsix.Composition;

internal static class BridgeServiceCollectionExtensions
{
    internal static IServiceCollection AddVsMcpBridgeServices(this IServiceCollection services, IAsyncPackage package)
    {
        services.AddSingleton(package);
        services.AddSingleton<IBridgeLogger, ActivityLogBridgeLogger>();
        services.AddSingleton<IUnhandledExceptionSink, FileUnhandledExceptionSink>();
        services.AddSingleton<IVsService, VsService>();
        services.AddSingleton<IPipeServer, PipeServer>();
        return services;
    }
}
