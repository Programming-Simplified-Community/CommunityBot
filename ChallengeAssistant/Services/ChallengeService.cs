using System.Net;
using ChallengeAssistant.Models;
using ChallengeAssistant.Requests;
using Core.Validation;
using Data;
using Data.Challenges;
using Discord;
using Discord.WebSocket;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Color = System.Drawing.Color;

namespace ChallengeAssistant.Services;

public record LeaderboardEntry(string Username, ProgrammingChallengeReport Report);
public class ChallengeService
{
    private readonly ILogger<ChallengeService> _logger;
    private readonly SocialDbContext _context;
    private readonly DiscordSocketClient _client;
    private readonly IServiceProvider _serviceProvider;
    
    public ChallengeService(ILogger<ChallengeService> logger, SocialDbContext context, DiscordSocketClient client, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _context = context;
        _client = client;
        _serviceProvider = serviceProvider;

        _client.ButtonExecuted += ChallengeButtonResponse;
        _client.ModalSubmitted += ChallengeModalResponse;
    }

    private async Task ChallengeModalResponse(SocketModal modal)
    {
        try
        {
            var components = modal.Data.Components.ToList();
            var runner = _serviceProvider.GetRequiredService<ICodeRunner>();
            var challengeId = modal.Data.CustomId.ExtractDiscordModalChallengeId();

            var user = await _context.GetOrAddUser(modal.User.Username, modal.User.Id.ToString());

            var challenge = await _context.ProgrammingChallenges
                .Include(x => x.Tests)
                .FirstOrDefaultAsync(x => x.Id == challengeId);

            if (challenge is null)
            {
                _logger.LogError("Was unable to locate challenge with Id {Id} for user {Username}",
                    challengeId,
                    modal.User.Username);
                return;
            }

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
    }
    
    private async Task ChallengeButtonResponse(SocketMessageComponent messageComponent)
    {
        var id = messageComponent.Data.CustomId.ExtractDiscordButtonChallengeId();
        var challenge = await _context.ProgrammingChallenges.FirstOrDefaultAsync(x => x.Id == id);

        if (challenge is null)
        {
            _logger.LogError("Unable to process button {ButtonId}'s response", messageComponent.Data.CustomId);
            return;
        }

        var modal = new ModalBuilder()
            .WithTitle(challenge.Title)
            .WithCustomId(challenge.ToDiscordModalId())
            .AddTextInput(new TextInputBuilder()
                .WithCustomId("code")
                .WithLabel("Code")
                .WithRequired(true)
                .WithStyle(TextInputStyle.Paragraph));

        await messageComponent.RespondWithModalAsync(modal.Build(), new()
        {
            RetryMode = RetryMode.AlwaysRetry,
            AuditLogReason = $"{messageComponent.User.Username} - attempted to solve {challenge.Title}"
        });
    }
    
    public async Task<List<LeaderboardEntry>> ViewLeaderboardFor(string title,
        CancellationToken cancellationToken = default)
    {
        var challenge =
            await _context.ProgrammingChallenges.FirstOrDefaultAsync(x => x.Title == title, cancellationToken);

        if (challenge is null)
            return new();

        var query = await (from report in _context.ChallengeReports
                join user in _context.Users
                    on report.UserId equals user.Id
                where report.ProgrammingChallengeId == challenge.Id
                select new LeaderboardEntry(user.UserName, report)
            ).ToListAsync(cancellationToken);
        return query.OrderByDescending(x=>x.Report.Points)
            .ToList();
    }

    public async Task<List<LeaderboardEntry>> ViewLeaderboard(CancellationToken cancellationToken = default)
    {
        var query = await (from report in _context.ChallengeReports
            join user in _context.Users
                on report.UserId equals user.Id
            select new LeaderboardEntry(user.UserName, report)).ToListAsync(cancellationToken);

        return query.OrderByDescending(x => x.Report.Points).ToList();
    }
    
    public async Task<List<ProgrammingChallenge>> GetAll(CancellationToken cancellationToken = default)
        => await _context.ProgrammingChallenges.ToListAsync(cancellationToken);

    public async Task<ProgrammingChallenge?> FindChallenge(int id, CancellationToken cancellationToken = default)
        => await _context.ProgrammingChallenges
            .Include(x=>x.Tests)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<ProgrammingChallenge?> FindChallenge(string title, CancellationToken cancellationToken = default)
        => await _context.ProgrammingChallenges
            .Include(x=>x.Tests)
            .FirstOrDefaultAsync(x => x.Title == title, cancellationToken);
    
    public async Task<List<ProgrammingChallenge>> FindChallengesWithLanguage(ProgrammingLanguage language,
        CancellationToken cancellationToken = default)
        => await _context.ProgrammingChallenges
            .Include(x => x.Tests)
            .Where(x => x.Tests.Any(y => y.Language == language))
            .ToListAsync(cancellationToken);

    public async Task<ResultOf<ProgrammingChallengeSubmission>> GetUserSubmissionFor(string discordUserId, int challengeId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(discordUserId) || challengeId <= 0)
            return ResultOf<ProgrammingChallengeSubmission>.Error(
                "Invalid discord or challenge Id");
        
        var user = await _context.Users.FirstOrDefaultAsync(x => x.DiscordUserId == discordUserId, cancellationToken);

        if (user is null)
            return ResultOf<ProgrammingChallengeSubmission>.NotFound("Doesn't seem you submitted anything before?");

        var existing = await _context.ProgrammingChallengeSubmissions.FirstOrDefaultAsync(
            x => x.ProgrammingChallengeId == challengeId && x.UserId == user.Id, cancellationToken);
        
        return existing is null 
            ? ResultOf<ProgrammingChallengeSubmission>.NotFound("Unable to find submission") 
            : ResultOf<ProgrammingChallengeSubmission>.Success(existing);
    }

    public async Task<ResultOf<HttpStatusCode>> Submit(SubmitProgrammingChallengeRequest request)
    {
        #region Validation

        var validationResult = Validator.Validate(request);

        if (!validationResult.IsValid)
        {
            var errors = string.Join(Environment.NewLine, validationResult.Errors);
            _logger.LogWarning("Invalid data was provided to {Name}: {Errors}", nameof(Submit), errors);
            return ResultOf<HttpStatusCode>.Error(errors);
        }

        var challengeItemTask =
            _context.ProgrammingChallenges.FirstOrDefaultAsync(x => x.Id == request.ProgrammingChallengeId);
        var userItemTask = _context.Users.FirstOrDefaultAsync(x => x.DiscordUserId == request.DiscordUserId);

        await Task.WhenAll(challengeItemTask, userItemTask);
        
        if(challengeItemTask.Result is null)
            return ResultOf<HttpStatusCode>.NotFound("Could not locate challenge");

        ProgrammingChallengeSubmission? submission = null;

        var userId = userItemTask.Result?.Id ?? string.Empty;
        
        if (userItemTask.Result is not null)
        {
            submission = await _context.ProgrammingChallengeSubmissions.FirstOrDefaultAsync(x =>
                x.ProgrammingChallengeId == request.ProgrammingChallengeId && x.UserId == userItemTask.Result.Id);
        }
        else
        {
            var user = new SocialUser
            {
                DiscordUserId = request.DiscordUserId,
                UserName = request.DiscordUsername,
                NormalizedUserName = request.DiscordUsername.ToUpper(),
                Email = "changeme@gmail.com",
                DiscordDisplayName = request.DiscordUsername
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            userId = user.Id; // update our cached id
        }
        
        #endregion

        if (submission is null)
        {
            submission = new()
            {
                UserId = userId,
                ProgrammingChallengeId = request.ProgrammingChallengeId,
                UserSubmission = request.Code
            };

            _context.ProgrammingChallengeSubmissions.Add(submission);
        }
        else
        {
            submission.UserSubmission = request.Code;
            submission.SubmittedOn = DateTime.UtcNow;
        }            
        
        await _context.SaveChangesAsync();
        return ResultOf<HttpStatusCode>.Success(HttpStatusCode.OK);
    }
}