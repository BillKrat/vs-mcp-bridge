using Adventures.ChatEngine.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using ChatEngineService = Adventures.ChatEngine.Services.ChatEngine;

namespace Adventures.ChatEngine.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddChatEngine(this IServiceCollection services)
    {
        services.AddSingleton<IChatEngine, ChatEngineService>();
        return services;
    }
}
