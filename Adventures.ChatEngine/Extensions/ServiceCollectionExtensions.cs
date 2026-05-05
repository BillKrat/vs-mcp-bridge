using Adventures.ChatEngine.Abstractions;
using Adventures.ChatEngine.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Adventures.ChatEngine.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddChatEngine(this IServiceCollection services)
    {
        services.AddSingleton<IChatEngine, ChatEngineService>();
        return services;
    }
}
