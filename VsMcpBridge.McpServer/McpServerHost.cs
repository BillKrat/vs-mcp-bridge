using Adventures.ChatEngine.Abstractions;
using Adventures.ChatEngine.Extensions;
using Adventures.ChatEngine.OpenAI.Extensions;
using Adventures.ChatEngine.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VsMcpBridge.McpServer.ChatEngine;
using VsMcpBridge.McpServer.Pipe;
using VsMcpBridge.McpServer.Tools;
using VsMcpBridge.Shared.Interfaces;
using VsMcpBridge.Shared.Loggers;

namespace VsMcpBridge.McpServer;

public static class McpServerHost
{
    private const string ProviderConfigurationKey = "Adventures:ChatEngine:Provider";

    public static void Configure(HostApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();

        IServiceCollection services = builder.Services;

        services
            .AddSingleton<ILogger, AppDataFolderLogger>()
            .AddSingleton<ILogger<ChatEngineService>>(serviceProvider =>
                new BridgeLoggerAdapter<ChatEngineService>(serviceProvider.GetRequiredService<ILogger>()))
            .AddSingleton<IHostedService, ChatEngineVerificationHostedService>()
            .AddChatEngine()
            .AddSingleton<IPipeClient, PipeClient>()
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithTools<VsTools>();

        string? provider = builder.Configuration[ProviderConfigurationKey];

        if (string.Equals(provider, "OpenAI", StringComparison.OrdinalIgnoreCase))
        {
            services.AddOpenAiProvider(builder.Configuration);
            return;
        }

        services.AddSingleton<IAiChatProvider, HostPingPongChatProvider>();
    }
}
