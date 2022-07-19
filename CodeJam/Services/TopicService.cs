using System.Net;
using CodeJam.Requests;
using Core.Validation;
using Data.CodeJam;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CodeJam.Services;

public class TopicService
{
    private readonly ILogger<TopicService> _logger;
    private readonly SocialDbContext _context;

    public TopicService(ILogger<TopicService> logger, SocialDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    /// <summary>
    /// Retrieve all the topics that are currently available for users to register for
    /// </summary>
    /// <returns></returns>
    public async Task<List<Topic>> GetRegisterableTopics()
    {
        var now = DateTime.UtcNow;

        return await _context.CodeJamTopics
            .Where(x => now >= x.RegistrationStartOn && now <= x.RegistrationEndOn)
            .ToListAsync();
    }

    /// <summary>
    /// Validate and update registration date range for a particular topic
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<ResultOf<HttpStatusCode>> UpdateRegistrationDateRange(
        UpdateTopicRegistrationDateRangeRequest request, CancellationToken cancellationToken = default)
    {
        var topic = await _context.CodeJamTopics.FirstOrDefaultAsync(x => x.Title == request.TopicTitle,
            cancellationToken);

        if (topic is null)
        {
            _logger.LogWarning("Was unable to locate topic: {Topic} to update date range for...", request.TopicTitle);
            return ResultOf<HttpStatusCode>.NotFound("Unable to locate topic");
        }

        topic.RegistrationStartOn = request.StartDate;
        topic.RegistrationEndOn = request.EndDate;

        await _context.SaveChangesAsync(cancellationToken);
        
        return ResultOf<HttpStatusCode>.Success(HttpStatusCode.OK);
    }

    /// <summary>
    /// Validate and update the submission date range for a particular topic
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<ResultOf<HttpStatusCode>> UpdateSubmissionDateRange(UpdateTopicSubmissionDateRangeRequest request,
        CancellationToken cancellationToken = default)
    {
        var topic = await _context.CodeJamTopics.FirstOrDefaultAsync(x => x.Title == request.TopicTitle,
            cancellationToken);

        if (topic is null)
        {
            _logger.LogWarning("Was unable to locate topic {Topic} to update submission range for...",
                request.TopicTitle);

            return ResultOf<HttpStatusCode>.NotFound("Unable to locate topic");
        }

        topic.StartDateOn = request.StartDate;
        topic.EndDateOn = request.EndDate;

        await _context.SaveChangesAsync(cancellationToken);
        
        return ResultOf<HttpStatusCode>.Success(HttpStatusCode.OK);
    }
}