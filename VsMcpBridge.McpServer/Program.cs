using Microsoft.Extensions.Hosting;
using VsMcpBridge.McpServer;

var builder = Host.CreateApplicationBuilder(args);
McpServerHost.Configure(builder);

await builder.Build().RunAsync();
