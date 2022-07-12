using System.Net;
using Core.Validation;
using Discord.WebSocket;
using DiscordHub;

namespace CodeJam.Services.InteractionHandlers;

[DiscordInteractionHandlerName("No Thanks", Constants.NO_THANKS_JAM_BUTTON_PREFIX)]
public class NoThanksCodeJamButtonInteractionHandler : IDiscordButtonHandler
{
    private readonly ILogger<NoThanksCodeJamButtonInteractionHandler> _logger;

    public NoThanksCodeJamButtonInteractionHandler(ILogger<NoThanksCodeJamButtonInteractionHandler> logger)
    {
        _logger = logger;
    }

    public async Task<ResultOf<HttpStatusCode>> HandleButton(SocketMessageComponent component)
    {
        var parts = component.Data.CustomId.Split('_');

        // Make sure this is the original user who is responding... not some other user
        if (parts[^1] != component.User.Id.ToString())
        {
            await component.RespondAsync(embed: Util
                .Embed("Oops", "This is not your message to interact with!", MessageType.Warning)
                .Build(), ephemeral: true);
            _logger.LogWarning("{Username} attempted to interact with user id's message {id}",
                component.User.Username, parts[^1]);
            return ResultOf<HttpStatusCode>.Success(HttpStatusCode.OK);
        }
        
        await component.Message.DeleteAsync();
        await component.RespondAsync(
            embed: Util.Embed("No worries!", "Please introduce yourself to the community!", MessageType.Info)
                .Build(),
            ephemeral: true);
        
        return ResultOf<HttpStatusCode>.Success(HttpStatusCode.OK);
    }
}