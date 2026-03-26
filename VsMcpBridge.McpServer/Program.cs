using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VsMcpBridge.McpServer.Pipe;
using VsMcpBridge.McpServer.Tools;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddSingleton<PipeClient>()
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<VsTools>();

await builder.Build().RunAsync();
