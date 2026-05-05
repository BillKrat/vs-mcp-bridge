using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VsMcpBridge.App.Services;
using VsMcpBridge.Shared.Constants;
using VsMcpBridge.Shared.Diagnostics;
using VsMcpBridge.Shared.Interfaces;
using VsMcpBridge.Shared.Loggers;
using VsMcpBridge.Shared.Services;

namespace VsMcpBridge.App.Composition;

internal static class BridgeServiceCollectionExtensions
{
    internal static IServiceCollection AddVsMcpBridgeAppServices(this IServiceCollection services, IConfiguration configuration)
    {
        var minimumLevel = ResolveMinimumLevel(configuration[ConfigurationKeys.LoggingMinimumLevel]);
        var provider = configuration[ConfigurationKeys.LoggingProvider];

        services.AddSingleton<IBridgeLogSink, BridgeLogSink>();
        services.AddSingleton<AppSessionState>();
        services.AddSingleton<IProposalDraftState>(serviceProvider => serviceProvider.GetRequiredService<AppSessionState>());
        services.AddSingleton<ILogLevelSettings>(_ => new LogLevelSettings { MinimumLevel = minimumLevel });
        services.AddSingleton<ILogger>(serviceProvider =>
        {
            var settings = serviceProvider.GetRequiredService<ILogLevelSettings>();
            var logSink = serviceProvider.GetRequiredService<IBridgeLogSink>();
            ILoggerProvider primaryProvider;

            if (string.Equals(provider, "StdErr", StringComparison.OrdinalIgnoreCase))
                primaryProvider = new StdErrLoggerProvider(settings);
            else
                primaryProvider = new DebugBridgeLoggerProvider(settings);

            var uiProvider = new UiBridgeLoggerProvider(logSink, settings);
            var primaryLogger = primaryProvider.CreateLogger(BridgeRuntimeConstants.LoggerCategory);
            var uiLogger = uiProvider.CreateLogger(BridgeRuntimeConstants.LoggerCategory);

            return new CompositeBridgeLogger(primaryLogger, uiLogger);
        });
        services.AddSingleton<IUnhandledExceptionSink, FileUnhandledExceptionSink>();
        services.AddSingleton<IApprovalWorkflowService, InMemoryApprovalWorkflowService>();
        services.AddSingleton<IEditApplier, FileEditApplier>();
        services.AddSingleton<IProposalFilePicker, ProposalFilePicker>();
        services.AddSingleton<IThreadHelper, DispatcherThreadHelper>();
        services.AddSingleton<IChatRequestService, AppChatRequestService>();
        services.AddSingleton<IVsService, StandaloneVsService>();
        services.AddSingleton<IPipeServer, PipeServer>();
        return services;
    }

    private static LogLevel ResolveMinimumLevel(string? configuredLevel)
    {
        if (!string.IsNullOrWhiteSpace(configuredLevel)
            && Enum.TryParse(configuredLevel, ignoreCase: true, out LogLevel parsedLevel))
            return parsedLevel;

        return LogLevel.Information;
    }
}
