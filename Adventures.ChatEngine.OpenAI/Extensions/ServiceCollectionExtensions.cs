using Adventures.ChatEngine.Abstractions;
using Adventures.ChatEngine.OpenAI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Adventures.ChatEngine.OpenAI.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOpenAiProvider(this IServiceCollection services)
    {
        services.AddSingleton<IAiChatProvider, OpenAiChatProvider>();
        return services;
    }
}
