using System.Net;
using ChallengeAssistant.Models;
using ChallengeAssistant.Requests;
using Core.Validation;
using Data.Challenges;
using Discord.WebSocket;
using Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ChallengeAssistant.Services;

/// <summary>
/// Item used to store data we want to display in our Leaderboard
/// </summary>
/// <param name="Username"></param>
/// <param name="Attempts"></param>
/// <param name="Points"></param>
/// <param name="Duration"></param>
public record LeaderboardEntry(string Username, string Avatar, int Attempts, int Points, double Duration);

public class ChallengeService
{
    private readonly ILogger<ChallengeService> _logger;
    private readonly SocialDbContext _context;
    private readonly IServiceProvider _serviceProvider;
    private readonly DiscordSocketClient _client;
    
    public ChallengeService(ILogger<ChallengeService> logger, SocialDbContext context, IServiceProvider serviceProvider, DiscordSocketClient client)
    {
        _logger = logger;
        _context = context;
        _serviceProvider = serviceProvider;
        _client = client;
    }

    public async Task<SubmissionCompareViewModel?> CompareMySubmissions(string discordId, ProgrammingLanguage language)
    {
        var callerUser = await _context.Users.FirstOrDefaultAsync(x => x.DiscordUserId == discordId);
        
        if (callerUser is null)
            return null;
        
        var query = await (from submission in _context.ProgrammingChallengeSubmissions
                join user in _context.Users
                    on submission.UserId equals user.Id
                join challenge in _context.ProgrammingChallenges
                    on submission.ProgrammingChallengeId equals challenge.Id
                where submission.SubmittedLanguage == language
                select new
                {
                    Username = user.UserName,
                    DiscordId = user.DiscordUserId,
                    challenge.Title,
                    submission.UserSubmission
                })
            .ToListAsync();

        var mySubmissions = query.Where(x => x.DiscordId == discordId)
            .ToDictionary(x => x.Title, x => new MySubmission(x.UserSubmission, language));

        // We require users to have a submission
        if (!mySubmissions.Any())
            return null;

        var myKeys = mySubmissions.Keys.ToHashSet();
        var otherSubmissions = query.Where(x => x.DiscordId != discordId && myKeys.Contains(x.Title))
            .GroupBy(x => x.Title)
            .ToDictionary(x => x.Key, x => x.ToList());

        Dictionary<string, List<UserSubmission>> subs = new();
        foreach (var title in otherSubmissions.Keys)
        {
            subs.Add(title, new());

            foreach (var item in otherSubmissions[title])
                subs[title].Add(new(item.Username, item.UserSubmission, language));
        }

        return new SubmissionCompareViewModel
        {
            MySubmissions = mySubmissions,
            UserSubmissions = subs
        };
    }

    /// <summary>
    /// Attempt to rerun a user's submission without having the user required to resubmit their code. Also doesn't impact their number of attempts
    /// because this is done by an administrator, not the user
    /// </summary>
    /// <param name="discordId">ID of the user whose submission we want to rerun</param>
    /// <param name="challengeTitle">Title of challenge we want to search by</param>
    /// <param name="language">The language in which the user submitted for</param>
    /// <returns>StatusCode indicating level of success</returns>
    public async Task<HttpStatusCode> RerunUserSubmission(string discordId, string challengeTitle, ProgrammingLanguage language)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.DiscordUserId == discordId);

        if (user is null)
            return HttpStatusCode.NotFound;

        var challenge = await _context.ProgrammingChallenges
            .Include(x=>x.Tests)
            .FirstOrDefaultAsync(x => x.Title == challengeTitle);

        if (challenge is null)
            return HttpStatusCode.NotFound;

        var submission = await _context.ProgrammingChallengeSubmissions.FirstOrDefaultAsync(x => x.UserId == user.Id &&
            x.ProgrammingChallengeId == challenge.Id &&
            x.SubmittedLanguage == language);

        if (submission is null)
            return HttpStatusCode.NotFound;
        
        var runner = _serviceProvider.GetRequiredService<ICodeRunner>();
        UserSubmissionQueueItem item = new(challenge.Tests.First(x=>x.Language == language), submission, submission.UserSubmission);
        await runner.Enqueue(item);
        return HttpStatusCode.OK;
    }
    
    /// <summary>
    /// Reduce a user's number of attempts by <paramref name="reduceBy"/> amount
    /// </summary>
    /// <param name="discordUserId">User who we want to help out</param>
    /// <param name="reduceBy">Amount in which we want to reduce by</param>
    /// <param name="challengeTitle">Title in which we are reducing attempts for</param>
    /// <param name="language">Filters the submission by language (can have multiple language attempts...)</param>
    /// <returns></returns>
    public async Task<HttpStatusCode> ReduceUserAttemptsBy(string discordUserId, int reduceBy, string challengeTitle, ProgrammingLanguage language)
    {
        var challenge = await _context.ProgrammingChallenges.FirstOrDefaultAsync(x => x.Title == challengeTitle);

        if (challenge is null)
            return HttpStatusCode.BadRequest;
        
        var user = await _context.Users.FirstOrDefaultAsync(x => x.DiscordUserId == discordUserId);

        if (user is null)
            return HttpStatusCode.BadRequest;

        var submission = await _context.ProgrammingChallengeSubmissions.FirstOrDefaultAsync(x => x.UserId == user.Id &&
            x.ProgrammingChallengeId == challenge.Id &&
            x.SubmittedLanguage == language);

        if (submission is null)
            return HttpStatusCode.NotFound;

        submission.Attempt -= Math.Max(submission.Attempt - reduceBy, 0);
        await _context.SaveChangesAsync();

        return HttpStatusCode.OK;
    }

    /// <summary>
    /// Overall leaderboard
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns>List of the top contestants</returns>
    public async Task<List<LeaderboardEntry>> ViewLeaderboard(CancellationToken cancellationToken = default)
    {
        var allsubs = await _context.ProgrammingChallengeSubmissions.ToListAsync(cancellationToken);
        var submissions =allsubs.GroupBy(x=>x.UserId).ToDictionary(x => x.Key, x => x.Sum(y => y.Attempt));

            var query = await (from report in _context.ChallengeReports
                join user in _context.Users
                    on report.UserId equals user.Id
                select new
                {
                    UserId = user.Id,
                    user.DiscordUserId,
                    report.Points,
                    report.Duration,
                    Report = report,
                    Username = user.UserName,
                    Avatar = string.Empty
                }
            ).ToListAsync(cancellationToken);

            var avatars = query.GroupBy(x => x.Username)
                .Select(x => x.FirstOrDefault())
                .ToDictionary(x => x.DiscordUserId, x => x.Avatar);

            foreach (var user in avatars)
                avatars[user.Key] = (await _client.GetUserAsync(ulong.Parse(user.Key))).GetAvatarUrl();
            
            return query.GroupBy(x => x.Username)
            .Select(x =>
                new LeaderboardEntry(x.Key,
                    avatars[x.First().DiscordUserId],
                    submissions[x.First().UserId], 
                    x.Sum(y => y.Points),
                    x.Sum(y => double.Parse(y.Duration)))
            )
            .OrderByDescending(x => x.Points)
            .ThenBy(x=>x.Attempts)
            .ThenBy(x=>x.Duration)
            .ToList();
    }
    
    /// <summary>
    /// Retrieve all programming challenges
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<List<ProgrammingChallenge>> GetAll(CancellationToken cancellationToken = default)
        => await _context.ProgrammingChallenges.Include(x=>x.Tests).ToListAsync(cancellationToken);

    /// <summary>
    /// Find a specific <see cref="ProgrammingChallenge"/> via <paramref name="id"/>
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<ProgrammingChallenge?> FindChallenge(int id, CancellationToken cancellationToken = default)
        => await _context.ProgrammingChallenges
            .Include(x=>x.Tests)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    /// <summary>
    /// Find a specific <see cref="ProgrammingChallenge"/> via <paramref name="title"/>
    /// </summary>
    /// <param name="title"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<ProgrammingChallenge?> FindChallenge(string title, CancellationToken cancellationToken = default)
        => await _context.ProgrammingChallenges
            .Include(x=>x.Tests)
            .FirstOrDefaultAsync(x => x.Title == title, cancellationToken);
    
    /// <summary>
    /// Find a specific <see cref="ProgrammingChallenge"/> via <paramref name="language"/>
    /// </summary>
    /// <param name="language"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<List<ProgrammingChallenge>> FindChallengesWithLanguage(ProgrammingLanguage language,
        CancellationToken cancellationToken = default)
        => await _context.ProgrammingChallenges
            .Include(x => x.Tests)
            .Where(x => x.Tests.Any(y => y.Language == language))
            .ToListAsync(cancellationToken);

    /// <summary>
    /// Retrieve submissions from user with <paramref name="discordUserId"/> for challenge with Id <paramref name="challengeId"/>
    /// </summary>
    /// <param name="discordUserId"></param>
    /// <param name="challengeId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Submit user provided code 
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<ResultOf<int>> Submit(SubmitProgrammingChallengeRequest request)
    {
        try
        {
            var runner = _serviceProvider.GetRequiredService<ICodeRunner>();
            
            // If the user does not already exist in our system we'll add them!
            var user = await _context.GetOrAddUser(request.DiscordUsername, request.DiscordUserId);

            var challenge = await _context.ProgrammingChallenges
                .Include(x => x.Tests)
                .FirstOrDefaultAsync(x => x.Id == request.ProgrammingChallengeId);
            
            // Ensure the challenge is one we actually have in store
            if (challenge is null)
            {
                _logger.LogError("Was unable to locate challenge with Id {Id} for user {Username}",
                    request.ProgrammingChallengeId,
                    request.DiscordUsername);
                return ResultOf<int>.NotFound("Could not locate challenge");
            }

            var existingSubmission = await _context.ProgrammingChallengeSubmissions
                .FirstOrDefaultAsync(x => x.UserId == user.Id && 
                                          x.ProgrammingChallengeId == challenge.Id &&
                                          x.SubmittedLanguage == request.Language);
            
            /*
                We don't need a billion submission records for the SAME language + challenge for a user. 
                Just update existing records when applicable to conserve DB space.
             */
            
            if (existingSubmission is not null)
            {
                existingSubmission.UserSubmission = request.Code;
                existingSubmission.Attempt++;
                existingSubmission.SubmittedOn = DateTime.UtcNow;
            }
            else
            {
                existingSubmission =new ProgrammingChallengeSubmission
                {
                    UserSubmission = request.Code,
                    SubmittedLanguage = request.Language,
                    ProgrammingChallengeId = challenge.Id,
                    UserId = user.Id,
                    DiscordGuildId = request.DiscordGuildId, 
                    DiscordChannelId = request.DiscordChannelId
                };
                
                _context.ProgrammingChallengeSubmissions.Add(existingSubmission);
            }

            await _context.SaveChangesAsync();
            
            UserSubmissionQueueItem item = new(challenge.Tests.First(x=>x.Language == request.Language), existingSubmission, request.Code);
            await runner.Enqueue(item);
            return ResultOf<int>.Success(runner.PendingSubmissions);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error occurred while processing Modal: {Exception}", ex);
            return ResultOf<int>.Error("Something went wrong: " + ex.Message);
        }
    }
}