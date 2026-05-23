using Adventures.Auth.LocalApi;
using VsMcpBridge.Shared.AdventuresAuth;
using VsMcpBridge.Shared.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ISecurityRedactor, BridgeSecurityRedactor>();
builder.Services.AddSingleton<IAdventuresAuthDecisionService, AdventuresAuthDecisionService>();
builder.Services.AddSingleton<IAdventuresAuthApiService, AdventuresAuthApiService>();

var app = builder.Build();

app.MapAdventuresAuthEndpoints();

app.Run();

public partial class Program
{
}
