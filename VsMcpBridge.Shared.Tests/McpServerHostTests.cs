using Adventures.ChatEngine.Abstractions;
using Adventures.ChatEngine.Models;
using Adventures.ChatEngine.OpenAI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using VsMcpBridge.McpServer;
using VsMcpBridge.McpServer.ChatEngine;
using VsMcpBridge.McpServer.Pipe;
using VsMcpBridge.McpServer.Tools;
using VsMcpBridge.Shared.Interfaces;
using VsMcpBridge.Shared.Loggers;
using VsMcpBridge.Shared.Models;
using Xunit;

namespace VsMcpBridge.Shared.Tests;

public sealed class McpServerHostTests
{
    private const string ProviderEnvironmentVariable = "Adventures__ChatEngine__Provider";
    private const string OpenAiUseRealApiEnvironmentVariable = "Adventures__ChatEngine__OpenAI__UseRealApi";
    private const string OpenAiApiKeyEnvironmentVariable = "Adventures__ChatEngine__OpenAI__ApiKey";
    private const string OpenAiModelEnvironmentVariable = "Adventures__ChatEngine__OpenAI__Model";
    private const string ChatEngineModelEnvironmentVariable = "Adventures__ChatEngine__Model";

    [Fact]
    public void VsTools_exposes_only_the_expected_mcp_tool_allowlist()
    {
        var toolNames = typeof(VsTools)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Select(method => method.GetCustomAttribute<McpServerToolAttribute>()?.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            new[]
            {
                "chat_engine_chat",
                "chat_engine_ping",
                "chat_engine_summarize",
                "vs_get_active_document",
                "vs_get_error_list",
                "vs_get_selected_text",
                "vs_list_solution_projects",
                "vs_propose_text_edit",
                "vs_propose_text_edits"
            },
            toolNames);
    }

    [Fact]
    public void PipeCommands_defines_only_the_expected_dispatch_allowlist()
    {
        var commandNames = typeof(PipeCommands)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && !field.IsInitOnly && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            new[]
            {
                "vs_get_active_document",
                "vs_get_error_list",
                "vs_get_selected_text",
                "vs_list_solution_projects",
                "vs_propose_text_edit"
            },
            commandNames);
    }

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
        await WithClearedChatEngineEnvironmentAsync(async () =>
        {
            var builder = Host.CreateApplicationBuilder([]);

            McpServerHost.Configure(builder);

            using var host = builder.Build();
            var services = host.Services;

            IAiChatProvider provider = services.GetRequiredService<IAiChatProvider>();
            IChatEngine chatEngine = services.GetRequiredService<IChatEngine>();

            ChatResponse response = await chatEngine.SendAsync(
                new ChatRequest("ping"),
                CancellationToken.None);

            Assert.IsType<HostPingPongChatProvider>(provider);
            Assert.Equal("pong", response.Message);
        });
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

    [Fact]
    public void Configure_when_provider_is_openai_allows_chat_engine_to_resolve_configuration_from_di()
    {
        var builder = Host.CreateApplicationBuilder([]);
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Adventures:ChatEngine:Provider"] = "OpenAI",
            ["Adventures:ChatEngine:OpenAI:Model"] = "test-model",
            ["Adventures:ChatEngine:Retry:MaxAttempts"] = "2",
        });

        McpServerHost.Configure(builder);

        using var host = builder.Build();
        var services = host.Services;

        IConfiguration configuration = services.GetRequiredService<IConfiguration>();
        IChatEngine chatEngine = services.GetRequiredService<IChatEngine>();

        Assert.Same(host.Services.GetRequiredService<IConfiguration>(), configuration);
        Assert.Equal("OpenAI", configuration["Adventures:ChatEngine:Provider"]);
        Assert.Equal("test-model", configuration["Adventures:ChatEngine:OpenAI:Model"]);
        Assert.Equal("2", configuration["Adventures:ChatEngine:Retry:MaxAttempts"]);
        Assert.NotNull(chatEngine);
    }

    private static async Task WithClearedChatEngineEnvironmentAsync(Func<Task> action)
    {
        var environmentVariableNames = new[]
        {
            ProviderEnvironmentVariable,
            OpenAiUseRealApiEnvironmentVariable,
            OpenAiApiKeyEnvironmentVariable,
            OpenAiModelEnvironmentVariable,
            ChatEngineModelEnvironmentVariable
        };

        var originalValues = environmentVariableNames.ToDictionary(
            environmentVariableName => environmentVariableName,
            environmentVariableName => Environment.GetEnvironmentVariable(environmentVariableName));

        try
        {
            foreach (var environmentVariableName in environmentVariableNames)
            {
                Environment.SetEnvironmentVariable(environmentVariableName, null);
            }

            await action();
        }
        finally
        {
            foreach (var originalValue in originalValues)
            {
                Environment.SetEnvironmentVariable(originalValue.Key, originalValue.Value);
            }
        }
    }
}
