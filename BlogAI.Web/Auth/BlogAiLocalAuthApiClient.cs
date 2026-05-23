using System.Globalization;
using System.Net.Http.Json;

namespace BlogAI.Web.Auth;

public sealed class BlogAiLocalAuthApiClient : IBlogAiLocalAuthApiClient
{
    public static readonly Uri LocalDevelopmentBaseAddress = new("http://127.0.0.1:5257");

    private readonly HttpClient _httpClient;

    public BlogAiLocalAuthApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public Task<BlogAiLocalAuthApiClientDecision> LoginAsync(
        BlogAiLocalAuthApiClientRequest request,
        CancellationToken cancellationToken = default)
    {
        return PostAsync("auth/login", request, cancellationToken);
    }

    public Task<BlogAiLocalAuthApiClientDecision> LogoutAsync(
        BlogAiLocalAuthApiClientRequest request,
        CancellationToken cancellationToken = default)
    {
        return PostAsync("auth/logout", request, cancellationToken);
    }

    public async Task<BlogAiLocalAuthApiClientDecision> GetCurrentPrincipalAsync(
        BlogAiLocalAuthApiClientRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var response = await _httpClient
            .GetAsync("auth/me" + CreateCurrentPrincipalQuery(request), cancellationToken)
            .ConfigureAwait(false);

        return await ReadDecisionAsync(response, cancellationToken).ConfigureAwait(false);
    }

    public Task<BlogAiLocalAuthApiClientDecision> ValidateAsync(
        BlogAiLocalAuthApiClientRequest request,
        CancellationToken cancellationToken = default)
    {
        return PostAsync("auth/validate", request, cancellationToken);
    }

    private async Task<BlogAiLocalAuthApiClientDecision> PostAsync(
        string path,
        BlogAiLocalAuthApiClientRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var response = await _httpClient
            .PostAsJsonAsync(path, request, cancellationToken)
            .ConfigureAwait(false);

        return await ReadDecisionAsync(response, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<BlogAiLocalAuthApiClientDecision> ReadDecisionAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        response.EnsureSuccessStatusCode();

        var decision = await response.Content
            .ReadFromJsonAsync<BlogAiLocalAuthApiClientDecision>(cancellationToken)
            .ConfigureAwait(false);

        return decision ?? throw new InvalidOperationException("AdventuresAuth local API returned an empty response.");
    }

    private static string CreateCurrentPrincipalQuery(BlogAiLocalAuthApiClientRequest request)
    {
        var parameters = new List<string>();

        AddParameter(parameters, "correlationId", request.CorrelationId);
        AddParameter(parameters, "requestId", request.RequestId);
        AddParameter(parameters, "authDecisionId", request.AuthDecisionId);
        AddParameter(parameters, "clientApplication", request.ClientApplication);
        AddParameter(parameters, "environment", request.Environment);
        AddParameter(parameters, "localSessionId", request.LocalSessionId);

        return parameters.Count == 0
            ? string.Empty
            : "?" + string.Join("&", parameters);
    }

    private static void AddParameter(List<string> parameters, string name, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        parameters.Add(string.Create(
            CultureInfo.InvariantCulture,
            $"{Uri.EscapeDataString(name)}={Uri.EscapeDataString(value)}"));
    }
}
