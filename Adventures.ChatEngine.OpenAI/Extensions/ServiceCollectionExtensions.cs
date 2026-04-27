using Adventures.ChatEngine.Abstractions;
using Adventures.ChatEngine.OpenAI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Adventures.ChatEngine.OpenAI.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOpenAiProvider(this IServiceCollection services)
    {
        services.AddHttpClient<OpenAiChatProvider>();
        services.AddTransient<IAiChatProvider>(serviceProvider => serviceProvider.GetRequiredService<OpenAiChatProvider>());
        return services;
    }
}
