using Adventures.ChatEngine.Abstractions;
using Adventures.ChatEngine.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Adventures.ChatEngine.OpenAI.Services;

public sealed class OpenAiChatProvider : IAiChatProvider
{
    private const string ApiKeyConfigurationKey = "Adventures:ChatEngine:OpenAI:ApiKey";
    private const string ModelConfigurationKey = "Adventures:ChatEngine:OpenAI:Model";

    private readonly string? apiKey;
    private readonly ILogger<OpenAiChatProvider> logger;
    private readonly string? model;

    public OpenAiChatProvider(ILogger<OpenAiChatProvider> logger, IConfiguration configuration)
    {
        this.logger = logger;
        this.apiKey = configuration[ApiKeyConfigurationKey];
        this.model = configuration[ModelConfigurationKey];
    }

    public Task<ChatResponse> SendAsync(ChatRequest request, CancellationToken cancellationToken)
    {
        this.ValidateConfiguration();

        this.logger.LogInformation(
            "Handling stub OpenAI chat request. ApiKeyConfigured: {ApiKeyConfigured}, ModelConfigured: {ModelConfigured}, Model: {Model}",
            !string.IsNullOrWhiteSpace(this.apiKey),
            !string.IsNullOrWhiteSpace(this.model),
            this.model);

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
