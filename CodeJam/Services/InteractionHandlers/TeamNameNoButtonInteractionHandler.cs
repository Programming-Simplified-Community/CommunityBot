using System.Net;
using Core.Validation;
using Discord;
using Discord.WebSocket;
using DiscordHub;

namespace CodeJam.Services.InteractionHandlers;

[DiscordInteractionHandlerName("Team Name No", Constants.TEAM_NAME_VOTE_NO_BUTTON_PREFIX)]
public class TeamNameNoButtonInteractionHandler : IDiscordButtonHandler
{
    private readonly ILogger<TeamNameNoButtonInteractionHandler> _logger;
    private readonly TeamNameService _service;
    
    public TeamNameNoButtonInteractionHandler(ILogger<TeamNameNoButtonInteractionHandler> logger, TeamNameService service)
    {
        _logger = logger;
        _service = service;
    }
    
    public async Task<ResultOf<HttpStatusCode>> HandleButton(SocketMessageComponent component)
    {
        if (!component.Data.CustomId.ExtractTeamVoteInfo(Constants.TEAM_NAME_VOTE_NO_BUTTON_PREFIX, out var teamInfo))
        {
            _logger.LogWarning("Error parsing custom Id: {CustomId}", component.Data.CustomId);
            await component.RespondAsync(ephemeral: true, text: "Error occurred while processing request");
            return ResultOf<HttpStatusCode>.Success(HttpStatusCode.OK);
        }
        
        await component.RespondAsync(ephemeral: true, text: "Processing");
        
        var response = await _service.AddOrUpdateUserVote(component.User.Id.ToString(),
            teamInfo!.Value.TeamId,
            teamInfo!.Value.TeamNameVoteId,
            false);
        
        if(response.Result is null)
            return ResultOf<HttpStatusCode>.Success(HttpStatusCode.OK);
        
        if (response.Result.StatusCode == HttpStatusCode.NotAcceptable)
        {
            await component.Message.DeleteAsync();
            await component.Channel.SendMessageAsync(embed:
                new EmbedBuilder()
                    .WithColor(Color.Red)
                    .WithTitle("Welp...")
                    .WithDescription($"Appears the name `{response.Message}` didn't sparkle across the board...")
                    .Build());
        }
        else if (response.Result.StatusCode == HttpStatusCode.Ambiguous)
        {
            await component.Message.DeleteAsync();
            await component.Channel.SendMessageAsync(embed: new EmbedBuilder()
                .WithColor(Color.Magenta)
                .WithTitle("Stalemate")
                .WithDescription("Appears the team is evenly split on this one!")
                .WithFooter(response.Message)
                .Build());
        }
        else
        {
            await component.Message.ModifyAsync(x =>
            {
                x.Content = response.Result.ToString();
            });
        }
        
        return ResultOf<HttpStatusCode>.Success(HttpStatusCode.OK);
    }
}