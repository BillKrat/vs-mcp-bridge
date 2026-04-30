using Adventures.ChatEngine.Abstractions;
using Adventures.ChatEngine.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

    [Fact]
    public async Task Configure_registers_chat_engine_and_fake_provider_ping_path()
    {
        var builder = Host.CreateApplicationBuilder([]);

        McpServerHost.Configure(builder);

        using var host = builder.Build();
        var services = host.Services;

        IChatEngine chatEngine = services.GetRequiredService<IChatEngine>();

        ChatResponse response = await chatEngine.SendAsync(
            new ChatRequest("ping"),
            CancellationToken.None);

        Assert.Equal("pong", response.Message);
    }
}
