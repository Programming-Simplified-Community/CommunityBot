using CodeJam.Interfaces;
using CodeJam.Services;
using CodeJam.Services.InteractionHandlers;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DiscordHub;
using MediatR;

namespace CodeJam;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCodeJam(this IServiceCollection services, IConfiguration configuration)
    {
        return services
            .AddSingleton<IDiscordButtonHandler, JoinCodeJamButtonInteractionHandler>()
            .AddSingleton<IDiscordButtonHandler, NoThanksCodeJamButtonInteractionHandler>()
            .AddSingleton<RegistrationService>()
            .AddSingleton<SubmissionService>()
            .AddSingleton<TeamCreationService>()
            .AddSingleton<TopicService>()
            .Configure<Settings>(options=>configuration.GetSection("CodeJamBot:Settings").Bind(options))
            .AddMediatR(typeof(Settings).Assembly)
            .AddSingleton(new DiscordSocketClient(new DiscordSocketConfig{GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildMembers}))
            .AddSingleton(x=>new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
            .AddSingleton<CommandHandler>()
            .AddSingleton<CodeJamBot>()
            .AddHostedService(x=>x.GetRequiredService<CodeJamBot>())
            .AddSingleton<IDiscordService>(x=>x.GetRequiredService<CodeJamBot>())
            .AddHostedService<JamScheduleService>();
    }
}