using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VsMcpBridge.McpServer.Pipe;
using VsMcpBridge.McpServer.Tools;
using VsMcpBridge.Shared.Interfaces;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddSingleton<IPipeClient, PipeClient>()
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<VsTools>();

await builder.Build().RunAsync();
// TEST