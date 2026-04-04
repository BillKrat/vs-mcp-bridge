using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Shell;
using VsMcpBridge.Shared.Diagnostics;
using VsMcpBridge.Shared.Interfaces;
using VsMcpBridge.Shared.Services;
using VsMcpBridge.Vsix.Logging;
using VsMcpBridge.Vsix.Services;

namespace VsMcpBridge.Vsix.Composition;

internal static class BridgeServiceCollectionExtensions
{
    internal static IServiceCollection AddVsMcpBridgeServices(this IServiceCollection services, IAsyncPackage package)
    {
        services.AddSingleton(package);
        if (package is AsyncPackage asyncPackage)
            services.AddSingleton(asyncPackage);

        services.AddSingleton<IBridgeLogger, ActivityLogBridgeLogger>();
        services.AddSingleton<IUnhandledExceptionSink, FileUnhandledExceptionSink>();
        services.AddSingleton<IThreadHelper, ThreadHelperAdapter>();
        services.AddSingleton<IVsService, VsService>();
        services.AddSingleton<IPipeServer, PipeServer>();
        return services;
    }
}
