using System.Net;
using ChallengeAssistant.Requests;
using Core.Validation;
using Data;
using Data.Challenges;
using Data.CodeJam;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChallengeAssistant.Services;

public record LeaderboardEntry(string Username, int Attempts, int Points, double Duration);
public class ChallengeService
{
    private readonly ILogger<ChallengeService> _logger;
    private readonly SocialDbContext _context;
    
    public ChallengeService(ILogger<ChallengeService> logger, SocialDbContext context)
    {
        _logger = logger;
        _context = context;
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
                    Points = report.Points,
                    Duration = report.Duration,
                    Report = report,
                    Username = user.UserName
                }
            ).ToListAsync(cancellationToken);

        return query.GroupBy(x => x.Username)
            .Select(x =>
                new LeaderboardEntry(x.Key, 
                    submissions[x.First().UserId], 
                    x.Sum(y => y.Points),
                    x.Sum(y => y.Duration))
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