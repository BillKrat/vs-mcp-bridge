using Adventures.ChatEngine.Abstractions;
using Adventures.ChatEngine.OpenAI.Configuration;
using Adventures.ChatEngine.OpenAI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Adventures.ChatEngine.OpenAI.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOpenAiProvider(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<OpenAiChatProviderOptions>(
            configuration.GetSection("Adventures:ChatEngine:OpenAI"));
        services.AddHttpClient<OpenAiChatProvider>();
        services.AddTransient<IAiChatProvider>(serviceProvider => serviceProvider.GetRequiredService<OpenAiChatProvider>());
        return services;
    }
}
