using ChallengeAssistant.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ChallengeAssistant;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCodeRunner(this IServiceCollection services, IConfiguration config)
    {
        return services.AddSingleton<ChallengeService>()
                .AddSingleton<ICodeRunner, CodeRunnerService>()
                .AddHostedService(x => x.GetRequiredService<ICodeRunner>());
    }
}