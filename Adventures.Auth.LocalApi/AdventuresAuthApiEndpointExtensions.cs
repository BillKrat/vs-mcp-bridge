using Microsoft.AspNetCore.Routing;

namespace Adventures.Auth.LocalApi;

public static class AdventuresAuthApiEndpointExtensions
{
    public static RouteGroupBuilder MapAdventuresAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/auth");

        group.MapPost("/login", AdventuresAuthEndpointHandlers.Login);
        group.MapPost("/logout", AdventuresAuthEndpointHandlers.Logout);
        group.MapGet("/me", AdventuresAuthEndpointHandlers.CurrentPrincipal);
        group.MapPost("/validate", AdventuresAuthEndpointHandlers.Validate);

        return group;
    }
}
