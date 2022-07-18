using System.Net;
using Core.Validation;
using Discord;
using Discord.WebSocket;
using DiscordHub;

namespace CodeJam.Services.InteractionHandlers;

[DiscordInteractionHandlerName("Team Name Yes", Constants.TEAM_NAME_VOTE_YES_BUTTON_PREFIX)]
public class TeamNameYesButtonInteractionHandler : IDiscordButtonHandler
{
    private readonly ILogger<TeamNameYesButtonInteractionHandler> _logger;
    private readonly TeamNameService _service;
    
    public TeamNameYesButtonInteractionHandler(ILogger<TeamNameYesButtonInteractionHandler> logger, TeamNameService service)
    {
        _logger = logger;
        _service = service;
    }
    
    public async Task<ResultOf<HttpStatusCode>> HandleButton(SocketMessageComponent component)
    {
        
        if (!component.Data.CustomId.ExtractTeamVoteInfo(Constants.TEAM_NAME_VOTE_YES_BUTTON_PREFIX, out var teamInfo))
        {
            _logger.LogWarning("Error parsing custom Id: {CustomId}", component.Data.CustomId);
            await component.RespondAsync(text: "An error occurred while processing request.", ephemeral: true);
            return ResultOf<HttpStatusCode>.Success(HttpStatusCode.OK);
        }
        
        await component.RespondAsync(ephemeral: true, text: "Processing");

        var response = await _service.AddOrUpdateUserVote(component.User.Id.ToString(),
            teamInfo!.Value.TeamId,
            teamInfo!.Value.TeamNameVoteId,
            true);

        if (response.Result is null)
            return ResultOf<HttpStatusCode>.Success(HttpStatusCode.OK);
        
        if (response.Result.StatusCode == HttpStatusCode.Created)
        {
            await component.Message.DeleteAsync();
            await component.Channel.SendMessageAsync(embed: new EmbedBuilder()
                .WithColor(Color.Green)
                .WithTitle("Success!")
                .WithDescription("Your team's information has been updated!")
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
                x.Content = "```yml\n" +
                            $"Yes: {response.Result.Yes}\n" +
                            $"No: {response.Result.No}\n" +
                            $"Pending: {response.Result.Pending}\n" +
                            "```";
            });
        }
        
        return ResultOf<HttpStatusCode>.Success(HttpStatusCode.OK);
    }
}