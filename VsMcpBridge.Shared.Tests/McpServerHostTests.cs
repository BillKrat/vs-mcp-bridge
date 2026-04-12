using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Linq;
using VsMcpBridge.McpServer;
using VsMcpBridge.McpServer.Pipe;
using VsMcpBridge.Shared.Interfaces;
using VsMcpBridge.Shared.Loggers;
using Xunit;

namespace VsMcpBridge.Shared.Tests;

public sealed class McpServerHostTests
{
    [Fact]
    public void Configure_clears_default_logging_providers_and_registers_pipe_client_logger_and_tools()
    {
        var builder = Host.CreateApplicationBuilder([]);

        McpServerHost.Configure(builder);

        var providerTypes = builder.Services
            .Where(descriptor => typeof(ILoggerProvider).IsAssignableFrom(descriptor.ServiceType))
            .Select(descriptor => descriptor.ImplementationType?.FullName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToList();

        Assert.DoesNotContain(providerTypes, name => name!.Contains("ConsoleLoggerProvider"));
        Assert.DoesNotContain(providerTypes, name => name!.Contains("DebugLoggerProvider"));

        using var host = builder.Build();
        var services = host.Services;

        Assert.IsType<AppDataFolderLogger>(services.GetRequiredService<ILogger>());
        Assert.IsType<PipeClient>(services.GetRequiredService<IPipeClient>());
    }
}
