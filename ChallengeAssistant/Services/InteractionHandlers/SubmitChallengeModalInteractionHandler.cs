using System.Net;
using ChallengeAssistant.Models;
using Core.Validation;
using Data.Challenges;
using Discord;
using Discord.WebSocket;
using DiscordHub;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ChallengeAssistant.Services.InteractionHandlers;

/// <summary>
/// Handles user interaction with the modal window used to ingest user-code.
/// </summary>
[DiscordInteractionHandlerName("Submit Challenge", Constants.CHALLENGE_MODAL_PREFIX)]
public class SubmitChallengeModalInteractionHandler : IDiscordModalHandler
{
    private readonly ILogger<SubmitChallengeModalInteractionHandler> _logger;
    private readonly ChallengeService _service;

    public SubmitChallengeModalInteractionHandler(ILogger<SubmitChallengeModalInteractionHandler> logger, ChallengeService service)
    {
        _logger = logger;
        _service = service;
    }

    public async Task<ResultOf<HttpStatusCode>> HandleModal(SocketModal modal)
    {
        if (!modal.Data.CustomId.ExtractChallengeInfo(Constants.CHALLENGE_MODAL_PREFIX, out var challengeInfo))
            return ResultOf<HttpStatusCode>.Success(HttpStatusCode.OK);
        
        var components = modal.Data.Components.ToList();

        try
        {
            var response = await _service.Submit(new()
            {
                ProgrammingChallengeId = challengeInfo!.Value.Id,
                Code = components.First(x => x.CustomId == "code").Value,
                Language = challengeInfo.Value.Language,
                DiscordUsername = modal.User.Username,
                DiscordUserId = modal.User.Id.ToString(),
                DiscordChannelId = modal.Channel.Id.ToString(),
                DiscordGuildId = modal.GuildId.ToString()!
            });

            if (response.StatusCode != HttpStatusCode.OK)
                return new ResultOf<HttpStatusCode>(response.StatusCode, response.Message, response.StatusCode);
            
            var e = new EmbedBuilder()
                .WithTitle("Processing Request")
                .WithDescription($"There are: {response.Result} submissions ahead of you.")
                .WithColor(Discord.Color.Purple)
                .WithFooter("Please be patient");
        
            await modal.RespondAsync(embed: e.Build(), ephemeral: true);
            return ResultOf<HttpStatusCode>.Success(response.StatusCode);

        }
        catch (Exception ex)
        {
            _logger.LogError("An error occurred while processing submission request from {Username}:\n{Error}",
                modal.User.Username,
                ex);
            
            return ResultOf<HttpStatusCode>.Error(ex.Message);
        }
    }
}