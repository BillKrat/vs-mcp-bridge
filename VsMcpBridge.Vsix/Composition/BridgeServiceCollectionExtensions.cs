using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Shell;
using VsMcpBridge.Vsix.Logging;
using VsMcpBridge.Vsix.Pipe;
using VsMcpBridge.Vsix.Services;

namespace VsMcpBridge.Vsix.Composition;

internal static class BridgeServiceCollectionExtensions
{
    internal static IServiceCollection AddVsMcpBridgeServices(this IServiceCollection services, AsyncPackage package)
    {
        services.AddSingleton<AsyncPackage>(package);
        services.AddSingleton(package);
        services.AddSingleton<IBridgeLogger, ActivityLogBridgeLogger>();
        services.AddSingleton<IVsService, VsService>();
        services.AddSingleton<IPipeServer, PipeServer>();
        return services;
    }
}
