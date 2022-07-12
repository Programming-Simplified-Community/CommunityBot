using System.Net;
using ChallengeAssistant.Models;
using Core.Validation;
using Data.Challenges;
using Discord;
using Discord.WebSocket;
using DiscordHub;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ChallengeAssistant.Services.InteractionHandlers;


[DiscordInteractionHandlerName("Submit Challenge", Constants.CHALLENGE_MODAL_PREFIX)]
public class SubmitChallengeModalInteractionHandler : IDiscordModalHandler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SubmitChallengeModalInteractionHandler> _logger;
    private readonly SocialDbContext _context;

    public SubmitChallengeModalInteractionHandler(IServiceProvider serviceProvider, ILogger<SubmitChallengeModalInteractionHandler> logger, SocialDbContext context)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _context = context;
    }

    public async Task<ResultOf<HttpStatusCode>> HandleModal(SocketModal modal)
    {
        try
        {
            if (!modal.Data.CustomId.ExtractFrom(Constants.CHALLENGE_MODAL_PREFIX, out var textId))
            {
                return ResultOf<HttpStatusCode>.Success(HttpStatusCode.OK);
            }

            int.TryParse(textId, out var challengeId);
            
            if(challengeId <= 0)
                return ResultOf<HttpStatusCode>.Success(HttpStatusCode.OK);
            
            var components = modal.Data.Components.ToList();
            var runner = _serviceProvider.GetRequiredService<ICodeRunner>();
            
            // If the user does not already exist in our system we'll add them!
            var user = await _context.GetOrAddUser(modal.User.Username, modal.User.Id.ToString());

            var challenge = await _context.ProgrammingChallenges
                .Include(x => x.Tests)
                .FirstOrDefaultAsync(x => x.Id == challengeId);
            
            // Ensure the challenge is one we actually have in store
            if (challenge is null)
            {
                _logger.LogError("Was unable to locate challenge with Id {Id} for user {Username}",
                    challengeId,
                    modal.User.Username);
                return ResultOf<HttpStatusCode>.Success(HttpStatusCode.OK);
            }
            
            // For now we only have 1 component in there, the code.
            var code = components.First(x => x.CustomId == "code").Value;
            
            var submission = new ProgrammingChallengeSubmission
            {
                UserSubmission = code,
                ProgrammingChallengeId = challengeId,
                DiscordGuildId = modal.GuildId?.ToString() ?? string.Empty,
                DiscordChannelId = modal.ChannelId?.ToString() ?? string.Empty,
                UserId = user.Id
            };

            _context.ProgrammingChallengeSubmissions.Add(submission);
            await _context.SaveChangesAsync();
            
            // Inform the user that their request is processing. Also letting them know how many others are waiting for their code to be tested
            var e = new EmbedBuilder()
                .WithTitle("Processing Request")
                .WithDescription($"There are: {runner.PendingSubmissions} submissions ahead of you.")
                .WithColor(Discord.Color.Purple)
                .WithFooter("Please be patient");
            
            await modal.RespondAsync(embed: e.Build(), ephemeral: true);
            
            UserSubmissionQueueItem item = new(challenge.Tests.First(), submission, code);
            await runner.Enqueue(item);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error occurred while processing Modal: {Exception}", ex);
            await modal.RespondAsync(ephemeral: true, text: "Error occurred while processing...");
        }
        
        return ResultOf<HttpStatusCode>.Success(HttpStatusCode.OK);
    }
}