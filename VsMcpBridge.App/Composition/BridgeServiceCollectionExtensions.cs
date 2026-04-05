using Microsoft.Extensions.DependencyInjection;
using VsMcpBridge.App.Services;
using VsMcpBridge.Shared.Diagnostics;
using VsMcpBridge.Shared.Interfaces;
using VsMcpBridge.Shared.Loggers;
using VsMcpBridge.Shared.Services;

namespace VsMcpBridge.App.Composition;

internal static class BridgeServiceCollectionExtensions
{
    internal static IServiceCollection AddVsMcpBridgeAppServices(this IServiceCollection services)
    {
        services.AddSingleton<AppSessionState>();
        services.AddSingleton<IProposalDraftState>(serviceProvider => serviceProvider.GetRequiredService<AppSessionState>());
        services.AddSingleton<IBridgeLogger, ConsoleBridgeLogger>();
        services.AddSingleton<IUnhandledExceptionSink, FileUnhandledExceptionSink>();
        services.AddSingleton<IApprovalWorkflowService, InMemoryApprovalWorkflowService>();
        services.AddSingleton<IEditApplier, FileEditApplier>();
        services.AddSingleton<IThreadHelper, DispatcherThreadHelper>();
        services.AddSingleton<IVsService, StandaloneVsService>();
        services.AddSingleton<IPipeServer, PipeServer>();
        return services;
    }
}
