using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CodeJam.Commands;

[Group("bot", "moderator commands")]
public class GlobalCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ILogger<GlobalCommands> _logger;
    private readonly IHostLifetime _lifetime;
    public GlobalCommands(ILogger<GlobalCommands> logger, IHostLifetime lifetime)
    {
        _logger = logger;
        _lifetime = lifetime;
    }

    [RequireUserPermission(GuildPermission.Administrator)]
    [SlashCommand("shutdown", "gracefully shutdown")]
    public async Task Shutdown()
    {
        await RespondAsync("Shutting bot down...");
        await _lifetime.StopAsync(default);
        Environment.Exit(0);
    }
}