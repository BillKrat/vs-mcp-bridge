using Adventures.ChatEngine.Abstractions;
using Adventures.ChatEngine.Models;
using Adventures.ChatEngine.OpenAI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
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
    public async Task Configure_when_provider_is_missing_uses_fake_provider()
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

    [Fact]
    public async Task Configure_when_provider_is_fake_uses_fake_provider()
    {
        var builder = Host.CreateApplicationBuilder([]);
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Adventures:ChatEngine:Provider"] = "Fake",
        });

        McpServerHost.Configure(builder);

        using var host = builder.Build();
        var services = host.Services;

        IAiChatProvider provider = services.GetRequiredService<IAiChatProvider>();
        IChatEngine chatEngine = services.GetRequiredService<IChatEngine>();

        ChatResponse response = await chatEngine.SendAsync(
            new ChatRequest("ping"),
            CancellationToken.None);

        Assert.Equal("HostPingPongChatProvider", provider.GetType().Name);
        Assert.Equal("pong", response.Message);
    }

    [Fact]
    public void Configure_when_provider_is_openai_registers_openai_provider_without_requiring_api_key_at_build_time()
    {
        var builder = Host.CreateApplicationBuilder([]);
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Adventures:ChatEngine:Provider"] = "OpenAI",
        });

        McpServerHost.Configure(builder);

        using var host = builder.Build();
        var services = host.Services;

        IAiChatProvider provider = services.GetRequiredService<IAiChatProvider>();
        IChatEngine chatEngine = services.GetRequiredService<IChatEngine>();

        Assert.IsType<OpenAiChatProvider>(provider);
        Assert.NotNull(chatEngine);
    }
}
