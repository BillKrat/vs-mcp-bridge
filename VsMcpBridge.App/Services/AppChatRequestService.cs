using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using VsMcpBridge.Shared.Interfaces;

namespace VsMcpBridge.App.Services;

internal sealed class AppChatRequestService : IChatRequestService
{
    private const string ProviderConfigurationKey = "Adventures:ChatEngine:Provider";
    private const string ApiKeyConfigurationKey = "Adventures:ChatEngine:OpenAI:ApiKey";
    private const string ModelConfigurationKey = "Adventures:ChatEngine:OpenAI:Model";
    private const string UseRealApiConfigurationKey = "Adventures:ChatEngine:OpenAI:UseRealApi";
    private const string ChatCompletionsEndpoint = "https://api.openai.com/v1/chat/completions";

    private static readonly HttpClient HttpClient = new();

    private readonly IConfiguration _configuration;
    private readonly ILogger _logger;

    public AppChatRequestService(IConfiguration configuration, ILogger logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> SendAsync(string message, CancellationToken cancellationToken = default)
    {
        var requestId = Guid.NewGuid().ToString("N");
        var stopwatch = Stopwatch.StartNew();
        var normalizedMessage = message?.Trim() ?? string.Empty;
        var provider = _configuration[ProviderConfigurationKey];

        _logger.LogInformation(
            "App chat request started [RequestId={RequestId}] [Provider={Provider}] [MessageLength={MessageLength}].",
            requestId,
            string.IsNullOrWhiteSpace(provider) ? "(missing)" : provider,
            normalizedMessage.Length);

        try
        {
            if (!string.Equals(provider, "OpenAI", StringComparison.OrdinalIgnoreCase))
            {
                var fakeResponse = string.Equals(normalizedMessage, "ping", StringComparison.OrdinalIgnoreCase)
                    ? "pong"
                    : $"echo:{normalizedMessage}";

                stopwatch.Stop();
                _logger.LogInformation(
                    "App chat request completed [RequestId={RequestId}] [Provider={Provider}] [ElapsedMs={ElapsedMs}] [ResponseLength={ResponseLength}].",
                    requestId,
                    string.IsNullOrWhiteSpace(provider) ? "(missing)" : provider,
                    stopwatch.ElapsedMilliseconds,
                    fakeResponse.Length);
                return fakeResponse;
            }

            var useRealApi = bool.TryParse(_configuration[UseRealApiConfigurationKey], out var parsedUseRealApi) && parsedUseRealApi;
            if (!useRealApi)
            {
                var stubResponse = string.Equals(normalizedMessage, "ping", StringComparison.OrdinalIgnoreCase)
                    ? "pong-from-openai"
                    : "openai-stub-response";

                stopwatch.Stop();
                _logger.LogInformation(
                    "App chat request completed in OpenAI stub mode [RequestId={RequestId}] [ElapsedMs={ElapsedMs}] [ResponseLength={ResponseLength}].",
                    requestId,
                    stopwatch.ElapsedMilliseconds,
                    stubResponse.Length);
                return stubResponse;
            }

            var apiKey = _configuration[ApiKeyConfigurationKey];
            var model = _configuration[ModelConfigurationKey] ?? _configuration["Adventures:ChatEngine:Model"];

            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(model))
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "App chat request missing OpenAI configuration [RequestId={RequestId}] [ElapsedMs={ElapsedMs}] [ApiKeyConfigured={ApiKeyConfigured}] [ModelConfigured={ModelConfigured}].",
                    requestId,
                    stopwatch.ElapsedMilliseconds,
                    !string.IsNullOrWhiteSpace(apiKey),
                    !string.IsNullOrWhiteSpace(model));
                return "OpenAI is selected, but ApiKey/Model configuration is missing.";
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, ChatCompletionsEndpoint)
            {
                Content = JsonContent.Create(new
                {
                    model,
                    messages = new[]
                    {
                        new { role = "user", content = normalizedMessage }
                    }
                })
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            using var response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "App chat request failed with non-success OpenAI status [RequestId={RequestId}] [ElapsedMs={ElapsedMs}] [StatusCode={StatusCode}] [ResponseSummary={ResponseSummary}].",
                    requestId,
                    stopwatch.ElapsedMilliseconds,
                    (int)response.StatusCode,
                    CreateSafeSummary(responseContent));
                return $"OpenAI request failed: {(int)response.StatusCode} {response.StatusCode}.";
            }

            var text = ExtractMessageContent(responseContent);
            if (string.IsNullOrWhiteSpace(text))
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "App chat request completed without OpenAI message content [RequestId={RequestId}] [ElapsedMs={ElapsedMs}].",
                    requestId,
                    stopwatch.ElapsedMilliseconds);
                return "OpenAI response did not contain message content.";
            }

            stopwatch.Stop();
            _logger.LogInformation(
                "App chat request completed [RequestId={RequestId}] [Provider=OpenAI] [ElapsedMs={ElapsedMs}] [ResponseLength={ResponseLength}].",
                requestId,
                stopwatch.ElapsedMilliseconds,
                text.Length);
            return text;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            _logger.LogWarning(
                "App chat request canceled [RequestId={RequestId}] [ElapsedMs={ElapsedMs}].",
                requestId,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "App chat request failed [RequestId={RequestId}] [ElapsedMs={ElapsedMs}] [Provider={Provider}].",
                requestId,
                stopwatch.ElapsedMilliseconds,
                string.IsNullOrWhiteSpace(provider) ? "(missing)" : provider);
            return "Chat request failed. Review bridge logs for details.";
        }
    }

    private static string CreateSafeSummary(string responseContent)
    {
        if (string.IsNullOrWhiteSpace(responseContent))
            return "<empty>";

        const int maxLength = 200;
        var trimmed = responseContent.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static string? ExtractMessageContent(string responseContent)
    {
        using JsonDocument document = JsonDocument.Parse(responseContent);

        if (!document.RootElement.TryGetProperty("choices", out var choices)
            || choices.ValueKind != JsonValueKind.Array
            || choices.GetArrayLength() == 0)
            return null;

        var firstChoice = choices[0];
        if (!firstChoice.TryGetProperty("message", out var message)
            || !message.TryGetProperty("content", out var content)
            || content.ValueKind != JsonValueKind.String)
            return null;

        return content.GetString();
    }
}
