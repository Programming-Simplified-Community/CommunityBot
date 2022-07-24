using ChallengeAssistant.Grpc;

namespace ChallengeAssistant;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCodeRunner(this IServiceCollection services, IConfiguration config)
    {
        // services.AddSingleton<Services.ChallengeService>()
        //         .AddSingleton<ICodeRunner, CodeRunnerService>()
        //         .AddHostedService(x => x.GetRequiredService<ICodeRunner>())
        //         .AddSingleton<IDiscordButtonHandler, AttemptChallengeButtonInteractionHandler>()
        //         .AddSingleton<IDiscordModalHandler, SubmitChallengeModalInteractionHandler>();
        
        services.AddRazorTemplating();
        
        return services;
    }

    public static IApplicationBuilder UseCodeRunner(this IApplicationBuilder app)
    {
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGrpcService<DummyImpl>();
        });
        
        return app;
    }
}