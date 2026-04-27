using Adventures.ChatEngine.Abstractions;
using Adventures.ChatEngine.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace Adventures.ChatEngine.OpenAI.Services;

public sealed class OpenAiChatProvider : IAiChatProvider
{
    private const string ApiKeyConfigurationKey = "Adventures:ChatEngine:OpenAI:ApiKey";
    private const string ModelConfigurationKey = "Adventures:ChatEngine:OpenAI:Model";
    private const string UseRealApiConfigurationKey = "Adventures:ChatEngine:OpenAI:UseRealApi";

    private readonly string? apiKey;
    private readonly HttpClient httpClient;
    private readonly ILogger<OpenAiChatProvider> logger;
    private readonly string? model;
    private readonly bool useRealApi;

    public OpenAiChatProvider(
        HttpClient httpClient,
        ILogger<OpenAiChatProvider> logger,
        IConfiguration configuration)
    {
        this.httpClient = httpClient;
        this.logger = logger;
        this.apiKey = configuration[ApiKeyConfigurationKey];
        this.model = configuration[ModelConfigurationKey];
        this.useRealApi = bool.TryParse(configuration[UseRealApiConfigurationKey], out bool configuredValue) && configuredValue;
    }

    public Task<ChatResponse> SendAsync(ChatRequest request, CancellationToken cancellationToken)
    {
        this.ValidateConfiguration();

        if (this.useRealApi)
        {
            throw new NotImplementedException("Real OpenAI HTTP integration has not been implemented yet.");
        }

        this.logger.LogInformation(
            "Handling stub OpenAI chat request. ApiKeyConfigured: {ApiKeyConfigured}, ModelConfigured: {ModelConfigured}, Model: {Model}",
            !string.IsNullOrWhiteSpace(this.apiKey),
            !string.IsNullOrWhiteSpace(this.model),
            this.model);

        _ = this.httpClient;

        string message = request.Message == "ping" ? "pong-from-openai" : "openai-stub-response";
        return Task.FromResult(new ChatResponse(message));
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(this.apiKey))
        {
            throw new InvalidOperationException($"Missing required configuration value: {ApiKeyConfigurationKey}");
        }

        if (string.IsNullOrWhiteSpace(this.model))
        {
            throw new InvalidOperationException($"Missing required configuration value: {ModelConfigurationKey}");
        }
    }
}
