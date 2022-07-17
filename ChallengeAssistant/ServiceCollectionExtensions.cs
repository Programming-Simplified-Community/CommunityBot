using ChallengeAssistant.Services;
using ChallengeAssistant.Services.InteractionHandlers;
using DiscordHub;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Razor.Templating.Core;

namespace ChallengeAssistant;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCodeRunner(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<ChallengeService>()
                .AddSingleton<ICodeRunner, CodeRunnerService>()
                .AddHostedService(x => x.GetRequiredService<ICodeRunner>())
                .AddSingleton<IDiscordButtonHandler, AttemptChallengeButtonInteractionHandler>()
                .AddSingleton<IDiscordModalHandler, SubmitChallengeModalInteractionHandler>();
        
        services.AddRazorTemplating();
        return services;
    }
}