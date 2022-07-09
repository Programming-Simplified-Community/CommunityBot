using System.Net;
using CodeJam.Requests;
using Core.Validation;
using Data.CodeJam;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CodeJam.Services;

public record SubmissionViewItem(Submission Submission, string Name, string Topic);

public class SubmissionService
{
    private readonly ILogger<SubmissionService> _logger;
    private readonly SocialDbContext _context;
    private readonly HttpClient _web = new();

    public SubmissionService(ILogger<SubmissionService> logger, SocialDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    /// <summary>
    /// Get a list of submissions
    /// </summary>
    /// <returns></returns>
    public async Task<List<SubmissionViewItem>> GetSubmissions()
    {
        var topics = await _context.CodeJamTopics.ToDictionaryAsync(x => x.Id, x => x.Title);
        var subs = await _context.CodeJamSubmissions.ToListAsync();
        var teams = await _context.CodeJamTeams.ToDictionaryAsync(x => x.Id, x => x.Name);

        var regs = await (from reg in _context.CodeJamRegistrations
            join user in _context.Users
                on reg.DiscordUserId equals user.DiscordUserId
            select new { MemberId = user.DiscordUserId, Name = user.DiscordDisplayName })
            .ToDictionaryAsync(x=>x.MemberId, x=>x.Name);

        List<SubmissionViewItem> results = new();

        foreach (var sub in subs)
        {
            var name = string.Empty;

            if (sub.TeamId.HasValue)
            {
                if (teams.ContainsKey(sub.TeamId.Value))
                    name = teams[sub.TeamId.Value];
                else
                    _logger.LogWarning("Submission with TeamId: {TeamId}, could not locate name", sub.TeamId.Value);
            }
            else if (sub.DiscordUserId is not null)
            {
                if (regs.ContainsKey(sub.DiscordUserId))
                    name = regs[sub.DiscordUserId];
                else
                    _logger.LogWarning("Submission with DiscordUserId: {MemberId} could not locate name",
                        sub.DiscordUserId);
            }

            if (string.IsNullOrEmpty(name)) continue;
            results.Add(new(sub, name, topics[sub.TopicId]));
        }

        return results;
    }

    public async Task<ResultOf<HttpStatusCode>> Submit(SubmissionRequest request,
        CancellationToken cancellationToken = default)
    {
        #region Validation

        var validationResult = Validator.Validate(request);

        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors);
            _logger.LogWarning(
                "User: {Username} attempting to submit repo '{Repo}' contains validation errors:\n{Errors}",
                request.DisplayName,
                request.GithubRepo,
                errors);
            return ResultOf<HttpStatusCode>.Error(errors);
        }

        var now = DateTime.Now;
        var existingRecord = await (from register in _context.CodeJamRegistrations
            join topic in _context.CodeJamTopics
                on register.TopicId equals topic.Id
            where register.DiscordGuildId == request.GuildId
                  && register.DiscordUserId == request.MemberId &&
                  now >= topic.StartDateOn &&
                  now <= topic.EndDateOn &&
                  topic.Title == request.Topic
            select new { topic, register }).FirstOrDefaultAsync(cancellationToken);

        if (existingRecord is null)
        {
            _logger.LogWarning("Member<{MemberId}> '{Username}' on Guild<{GuildId}> could not locate jam. {Repo} - for topic {Topic}",
                request.MemberId,
                request.DisplayName,
                request.GuildId,
                request.GithubRepo,
                request.Topic);

            validationResult.Errors.Add(
                "Either could not locate jam you're in, or you were outside the window to turn in");
        }
        
        // we need to see if the repository is publicly available
        if (request.GithubRepo.ToLower().StartsWith("https://github.com/"))
        {
            var repoResponse = await _web.GetAsync(new Uri(request.GithubRepo), cancellationToken);

            if (repoResponse.StatusCode == HttpStatusCode.NotFound)
                validationResult.Errors.Add(
                    "Unable to view repository. Please make sure the repository is set to public, and that you provided the correct repo url");
        }
        else validationResult.Errors.Add("Invalid Github URL");

        if (validationResult.Errors.Any())
            return validationResult.ExitWith(_logger);
        
        #endregion

        _context.CodeJamSubmissions.Add(new()
        {
            GithubRepo = request.GithubRepo,
            DiscordUserId = request.MemberId,
            TeamId = existingRecord.register.TeamId,
            TopicId = existingRecord.topic.Id
        });

        await _context.SaveChangesAsync(cancellationToken);
        return ResultOf<HttpStatusCode>.Success(HttpStatusCode.Created);
    }
}