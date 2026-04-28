using Adventures.ChatEngine.Abstractions;
using Adventures.ChatEngine.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Adventures.ChatEngine.OpenAI.Services;

public sealed class OpenAiChatProvider : IAiChatProvider
{
    private const string ApiKeyConfigurationKey = "Adventures:ChatEngine:OpenAI:ApiKey";
    private const string ModelConfigurationKey = "Adventures:ChatEngine:OpenAI:Model";
    private const string UseRealApiConfigurationKey = "Adventures:ChatEngine:OpenAI:UseRealApi";
    private const string ChatCompletionsEndpoint = "https://api.openai.com/v1/chat/completions";

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

    public async Task<ChatResponse> SendAsync(ChatRequest request, CancellationToken cancellationToken)
    {
        this.ValidateConfiguration();

        if (this.useRealApi)
        {
            return await this.SendRealRequestAsync(request, cancellationToken).ConfigureAwait(false);
        }

        this.logger.LogInformation(
            "Handling stub OpenAI chat request. ApiKeyConfigured: {ApiKeyConfigured}, ModelConfigured: {ModelConfigured}, Model: {Model}",
            !string.IsNullOrWhiteSpace(this.apiKey),
            !string.IsNullOrWhiteSpace(this.model),
            this.model);

        _ = this.httpClient;

        string message = request.Message == "ping" ? "pong-from-openai" : "openai-stub-response";
        return new ChatResponse(message);
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

    private async Task<ChatResponse> SendRealRequestAsync(ChatRequest request, CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, ChatCompletionsEndpoint)
        {
            Content = JsonContent.Create(new
            {
                model = this.model,
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = request.Message,
                    },
                },
            }),
        };

        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", this.apiKey);

        using HttpResponseMessage response = await this.httpClient
            .SendAsync(httpRequest, cancellationToken)
            .ConfigureAwait(false);

        string responseContent = await response.Content
            .ReadAsStringAsync(cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"OpenAI request failed with status code {(int)response.StatusCode} ({response.StatusCode}). Response: {CreateSafeSummary(responseContent)}");
        }

        string? content = ExtractMessageContent(responseContent);

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("OpenAI response did not contain choices[0].message.content.");
        }

        return new ChatResponse(content);
    }

    private static string CreateSafeSummary(string responseContent)
    {
        if (string.IsNullOrWhiteSpace(responseContent))
        {
            return "<empty>";
        }

        const int MaxLength = 200;
        string trimmed = responseContent.Trim();
        return trimmed.Length <= MaxLength ? trimmed : trimmed[..MaxLength];
    }

    private static string? ExtractMessageContent(string responseContent)
    {
        using JsonDocument document = JsonDocument.Parse(responseContent);

        if (!document.RootElement.TryGetProperty("choices", out JsonElement choices) ||
            choices.ValueKind != JsonValueKind.Array ||
            choices.GetArrayLength() == 0)
        {
            return null;
        }

        JsonElement firstChoice = choices[0];

        if (!firstChoice.TryGetProperty("message", out JsonElement message) ||
            !message.TryGetProperty("content", out JsonElement content) ||
            content.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return content.GetString();
    }
}
