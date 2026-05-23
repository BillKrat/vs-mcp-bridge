using BlogAI.Web.Auth;
using BlogAI.Web.Components;
using VsMcpBridge.Shared.Composition;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBlogAiAuthConsumerServices();
builder.Services.AddBlogAiLocalAuthStatusServices();
builder.Services.AddRazorComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>();

app.Run();
