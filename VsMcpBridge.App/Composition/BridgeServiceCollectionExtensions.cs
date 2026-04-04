using Microsoft.Extensions.DependencyInjection;
using VsMcpBridge.App.Logging;
using VsMcpBridge.App.Services;
using VsMcpBridge.Shared.Diagnostics;
using VsMcpBridge.Shared.Interfaces;
using VsMcpBridge.Shared.Services;

namespace VsMcpBridge.App.Composition;

internal static class BridgeServiceCollectionExtensions
{
    internal static IServiceCollection AddVsMcpBridgeAppServices(this IServiceCollection services)
    {
        services.AddSingleton<IBridgeLogger, ConsoleBridgeLogger>();
        services.AddSingleton<IUnhandledExceptionSink, FileUnhandledExceptionSink>();
        services.AddSingleton<IApprovalWorkflowService, InMemoryApprovalWorkflowService>();
        services.AddSingleton<IEditApplier, NullEditApplier>();
        services.AddSingleton<IThreadHelper, DispatcherThreadHelper>();
        services.AddSingleton<IVsService, NullVsService>();
        return services;
    }
}
