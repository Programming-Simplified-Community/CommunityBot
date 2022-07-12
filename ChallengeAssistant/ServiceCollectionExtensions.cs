using ChallengeAssistant.Services;
using ChallengeAssistant.Services.InteractionHandlers;
using DiscordHub;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ChallengeAssistant;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCodeRunner(this IServiceCollection services, IConfiguration config)
    {
        return services.AddSingleton<ChallengeService>()
                .AddSingleton<ICodeRunner, CodeRunnerService>()
                .AddHostedService(x => x.GetRequiredService<ICodeRunner>())
                .AddSingleton<IDiscordButtonHandler, AttemptChallengeButtonInteractionHandler>()
                .AddSingleton<IDiscordModalHandler, SubmitChallengeModalInteractionHandler>();
    }
}