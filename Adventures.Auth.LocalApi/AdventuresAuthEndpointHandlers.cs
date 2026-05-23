namespace Adventures.Auth.LocalApi;

public static class AdventuresAuthEndpointHandlers
{
    public static IResult Login(
        AdventuresAuthApiRequest request,
        IAdventuresAuthApiService service)
    {
        return Results.Ok(service.Login(request));
    }

    public static IResult Logout(
        AdventuresAuthApiRequest request,
        IAdventuresAuthApiService service)
    {
        return Results.Ok(service.Logout(request));
    }

    public static IResult CurrentPrincipal(
        string? correlationId,
        string? requestId,
        string? authDecisionId,
        string? clientApplication,
        string? environment,
        string? localSessionId,
        IAdventuresAuthApiService service)
    {
        return Results.Ok(service.CurrentPrincipal(new AdventuresAuthApiRequest
        {
            CorrelationId = correlationId,
            RequestId = requestId,
            AuthDecisionId = authDecisionId,
            ClientApplication = clientApplication,
            Environment = environment,
            LocalSessionId = localSessionId
        }));
    }

    public static IResult Validate(
        AdventuresAuthApiRequest request,
        IAdventuresAuthApiService service)
    {
        return Results.Ok(service.Validate(request));
    }
}
