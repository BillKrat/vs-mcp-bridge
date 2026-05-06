using Microsoft.Extensions.Hosting;
using VsMcpBridge.McpServer;
using VsMcpBridge.Shared.Configuration;

var builder = Host.CreateApplicationBuilder(args);
BridgeConfigurationFactory.AddDefaultSources(builder.Configuration, BridgeConfigurationFactory.GetDefaultUserSettingsFilePath());
McpServerHost.Configure(builder);

await builder.Build().RunAsync();
