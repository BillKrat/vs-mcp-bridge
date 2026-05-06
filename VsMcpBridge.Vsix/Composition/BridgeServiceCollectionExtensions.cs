using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Shell;
using System;
using VsMcpBridge.Shared.Constants;
using VsMcpBridge.Shared.Diagnostics;
using VsMcpBridge.Shared.Interfaces;
using VsMcpBridge.Shared.Loggers;
using VsMcpBridge.Shared.Services;
using VsMcpBridge.Vsix.Logging;
using VsMcpBridge.Vsix.Services;

namespace VsMcpBridge.Vsix.Composition;

internal static class BridgeServiceCollectionExtensions
{
    internal static IServiceCollection AddVsMcpBridgeServices(this IServiceCollection services, IAsyncPackage package, IConfiguration configuration)
    {
        var minimumLevel = ResolveMinimumLevel(configuration[ConfigurationKeys.LoggingMinimumLevel]);
        var provider = configuration[ConfigurationKeys.LoggingProvider];

        services.AddSingleton(package);
        if (package is AsyncPackage asyncPackage)
            services.AddSingleton(asyncPackage);

        services.AddSingleton(configuration);

        services.AddSingleton<IBridgeLogForwarder, FileBridgeLogForwarder>();
        services.AddSingleton<IBridgeLogSink, BridgeLogSink>();
        services.AddSingleton<ILogLevelSettings>(_ => new LogLevelSettings { MinimumLevel = minimumLevel });
        services.AddSingleton<ILogger>(serviceProvider =>
        {
            var settings = serviceProvider.GetRequiredService<ILogLevelSettings>();
            var logSink = serviceProvider.GetRequiredService<IBridgeLogSink>();
            ILoggerProvider primaryProvider;

            if (string.Equals(provider, ConfigurationKeys.LoggingProviderStdErr, StringComparison.OrdinalIgnoreCase))
                primaryProvider = new StdErrLoggerProvider(settings);
            else
                primaryProvider = new ActivityLogBridgeLoggerProvider(settings);

            var uiProvider = new UiBridgeLoggerProvider(logSink, settings);
            var primaryLogger = primaryProvider.CreateLogger(BridgeRuntimeConstants.LoggerCategory);
            var uiLogger = uiProvider.CreateLogger(BridgeRuntimeConstants.LoggerCategory);

            return new CompositeBridgeLogger(primaryLogger, uiLogger);
        });
        services.AddSingleton<IUnhandledExceptionSink, FileUnhandledExceptionSink>();
        services.AddSingleton<IApprovalWorkflowService, InMemoryApprovalWorkflowService>();
        services.AddSingleton<IEditApplier, VsixEditApplier>();
        services.AddSingleton<IProposalFilePicker, ProposalFilePicker>();
        services.AddSingleton<IThreadHelper, ThreadHelperAdapter>();
        services.AddSingleton<IChatRequestService, VsixChatRequestService>();
        services.AddSingleton<IVsService, VsService>();
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
