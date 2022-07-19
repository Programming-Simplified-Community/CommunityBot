using System.Net;
using Core.Validation;
using Discord;
using Discord.WebSocket;
using DiscordHub;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChallengeAssistant.Services.InteractionHandlers;

/// <summary>
/// Handles user-interactions for when they want to attempt a coding-challenge
/// </summary>
[DiscordInteractionHandlerName("Attempt Challenge", Constants.ATTEMPT_BUTTON_PREFIX)]
public class AttemptChallengeButtonInteractionHandler : IDiscordButtonHandler
{
    private readonly ILogger<AttemptChallengeButtonInteractionHandler> _logger;
    private readonly SocialDbContext _context;

    public AttemptChallengeButtonInteractionHandler(ILogger<AttemptChallengeButtonInteractionHandler> logger, SocialDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<ResultOf<HttpStatusCode>> HandleButton(SocketMessageComponent component)
    {
        if(!component.Data.CustomId.ExtractChallengeInfo(Constants.ATTEMPT_BUTTON_PREFIX, out var challengeInfo))
            return ResultOf<HttpStatusCode>.Success(HttpStatusCode.OK);
        
        var challenge = await _context.ProgrammingChallenges.FirstOrDefaultAsync(x => x.Id == challengeInfo!.Value.Id);

        if (challenge is null)
        {
            _logger.LogError("Unable to process button {ButtonId}'s response", component.Data.CustomId);
            return ResultOf<HttpStatusCode>.Success(HttpStatusCode.OK);
        }
        
        var modal = new ModalBuilder()
            .WithTitle(challenge.Title)
            .WithCustomId(string.Format(Constants.CHALLENGE_MODAL_NAME_FORMAT, challengeInfo!.Value.Language.ToString(), challenge.Id))
            .AddTextInput(new TextInputBuilder()
                .WithCustomId("code")
                .WithLabel("Code")
                .WithRequired(true)
                .WithStyle(TextInputStyle.Paragraph));

        await component.RespondWithModalAsync(modal.Build(), new()
        {
            RetryMode = RetryMode.AlwaysRetry,
            AuditLogReason = $"{component.User.Username} - attempted to solve {challenge.Title}"
        });
        
        return ResultOf<HttpStatusCode>.Success(HttpStatusCode.OK);
    }
}