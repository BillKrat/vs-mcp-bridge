using Adventures.ChatEngine.Abstractions;
using Adventures.ChatEngine.Models;
using Adventures.ChatEngine.OpenAI.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO;
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

    private readonly HttpClient httpClient;
    private readonly ILogger<OpenAiChatProvider> logger;
    private readonly OpenAiChatProviderOptions options;

    public OpenAiChatProvider(
        HttpClient httpClient,
        ILogger<OpenAiChatProvider> logger,
        IOptions<OpenAiChatProviderOptions> options)
    {
        this.httpClient = httpClient;
        this.logger = logger;
        this.options = options.Value;
    }

    public async Task<ChatResponse> SendAsync(ChatRequest request, CancellationToken cancellationToken)
    {
        this.ValidateConfiguration();

        if (this.options.UseRealApi)
        {
            return await this.SendRealRequestAsync(request, cancellationToken).ConfigureAwait(false);
        }

        this.logger.LogInformation(
            "Handling stub OpenAI chat request. ApiKeyConfigured: {ApiKeyConfigured}, ModelConfigured: {ModelConfigured}, Model: {Model}",
            !string.IsNullOrWhiteSpace(this.options.ApiKey),
            !string.IsNullOrWhiteSpace(this.options.Model),
            this.options.Model);

        _ = this.httpClient;

        string message = request.Message == "ping" ? "pong-from-openai" : "openai-stub-response";
        return new ChatResponse(message);
    }

    public async IAsyncEnumerable<ChatResponse> StreamAsync(
        ChatRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        this.ValidateConfiguration();

        if (!this.options.UseRealApi)
        {
            yield return await this.SendAsync(request, cancellationToken).ConfigureAwait(false);
            yield break;
        }

        await foreach (ChatResponse chunk in this.StreamRealRequestAsync(request, cancellationToken).ConfigureAwait(false))
        {
            yield return chunk;
        }
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(this.options.ApiKey))
        {
            throw new InvalidOperationException($"Missing required configuration value: {ApiKeyConfigurationKey}");
        }

        if (string.IsNullOrWhiteSpace(this.options.Model))
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
                model = this.options.Model,
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

        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", this.options.ApiKey);

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

    private async IAsyncEnumerable<ChatResponse> StreamRealRequestAsync(
        ChatRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, ChatCompletionsEndpoint)
        {
            Content = JsonContent.Create(new
            {
                model = this.options.Model,
                stream = true,
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

        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", this.options.ApiKey);
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        using HttpResponseMessage response = await this.httpClient
            .SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            string errorContent = await response.Content
                .ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);

            throw new InvalidOperationException(
                $"OpenAI request failed with status code {(int)response.StatusCode} ({response.StatusCode}). Response: {CreateSafeSummary(errorContent)}");
        }

        await using Stream responseStream = await response.Content
            .ReadAsStreamAsync(cancellationToken)
            .ConfigureAwait(false);
        using var reader = new StreamReader(responseStream);

        bool emittedChunk = false;
        bool receivedDone = false;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string? line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);

            if (line is null)
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data:", StringComparison.Ordinal))
            {
                continue;
            }

            string payload = line["data:".Length..].Trim();

            if (payload == "[DONE]")
            {
                receivedDone = true;
                break;
            }

            string? content = ExtractDeltaContent(payload);

            if (!string.IsNullOrWhiteSpace(content))
            {
                emittedChunk = true;
                yield return new ChatResponse(content);
            }
        }

        if (receivedDone && !emittedChunk)
        {
            throw new InvalidOperationException("OpenAI streaming response did not contain choices[0].delta.content.");
        }

        if (!receivedDone && !cancellationToken.IsCancellationRequested)
        {
            throw new InvalidOperationException("OpenAI streaming response ended before [DONE] was received.");
        }
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

    private static string? ExtractDeltaContent(string responseContent)
    {
        using JsonDocument document = JsonDocument.Parse(responseContent);

        if (!document.RootElement.TryGetProperty("choices", out JsonElement choices) ||
            choices.ValueKind != JsonValueKind.Array ||
            choices.GetArrayLength() == 0)
        {
            return null;
        }

        JsonElement firstChoice = choices[0];

        if (!firstChoice.TryGetProperty("delta", out JsonElement delta) ||
            !delta.TryGetProperty("content", out JsonElement content) ||
            content.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return content.GetString();
    }
}
