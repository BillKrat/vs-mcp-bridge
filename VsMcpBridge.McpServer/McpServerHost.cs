using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VsMcpBridge.McpServer.Pipe;
using VsMcpBridge.McpServer.Tools;
using VsMcpBridge.Shared.Interfaces;
using VsMcpBridge.Shared.Loggers;

namespace VsMcpBridge.McpServer;

public static class McpServerHost
{
    public static void Configure(HostApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();

        builder.Services
            .AddSingleton<ILogger, AppDataFolderLogger>()
            .AddSingleton<IPipeClient, PipeClient>()
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithTools<VsTools>();
    }
}
