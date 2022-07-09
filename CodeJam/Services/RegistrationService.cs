using System.Net;
using CodeJam.Requests;
using Core;
using Core.Validation;
using Data;
using Data.CodeJam;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CodeJam.Services;

public class RegistrationService
{
    private readonly ILogger<RegistrationService> _logger;
    private readonly SocialDbContext _context;

    public RegistrationService(ILogger<RegistrationService> logger, SocialDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    /// <summary>
    /// View topics that are currently accepting registrations
    /// </summary>
    /// <param name="referenceTime"></param>
    /// <returns></returns>
    public async Task<List<Topic>> ViewRegisterableTopics(DateTime? referenceTime = null)
    {
        referenceTime ??= DateTime.Now;

        return await _context.CodeJamTopics.Where(x =>
            referenceTime >= x.RegistrationStartOn && referenceTime <= x.RegistrationEndOn && x.IsActive).ToListAsync();
    }

    /// <summary>
    /// Get registration records that have no yet been confirmed
    /// </summary>
    /// <param name="topicId"></param>
    /// <returns></returns>
    public async Task<List<Registration>> GetRegistrationsWhoNeedConfirmation(int topicId)
    => await _context.CodeJamRegistrations
            .Where(x => x.TopicId == topicId &&
                        x.ConfirmedOn == null)
            .ToListAsync();
    
    /// <summary>
    /// Process a registration request. We may also add a new user, if this user is not already in our system
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<ResultOf<HttpStatusCode>> Register(RegistrationRequest request,
        CancellationToken cancellationToken = default)
    {
        #region Validation

        var validationResult = Validator.Validate(request);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Invalid data was provided to {Name}: {Errors}", "Register", string.Join(", ", validationResult.Errors));
            return ResultOf<HttpStatusCode>.Error(string.Join("\n", validationResult.Errors));
        }

        var now = DateTime.Now;

        var existingRegistration = await (
            from registration in _context.CodeJamRegistrations
            join topic in _context.CodeJamTopics
                on registration.TopicId equals topic.Id
            where topic.IsActive &&
                  topic.Title == request.TopicTitle &&

                  // matches the guild + user
                  registration.DiscordGuildId == request.GuildId &&
                  registration.DiscordUserId == request.MemberId
            select registration).FirstOrDefaultAsync(cancellationToken);

        var topicItem = await _context.CodeJamTopics.FirstOrDefaultAsync(
            x => x.IsActive && request.TopicTitle == x.Title && now >= x.RegistrationStartOn &&
                 now <= x.RegistrationEndOn, cancellationToken);

        var timezoneItem =
            await _context.CodeJamTimezones.FirstOrDefaultAsync(x => x.Name == request.Timezone, cancellationToken);

        if (existingRegistration is not null)
        {
            _logger.LogWarning("User<{MemberId}> '{Username}' on Guild<{GuildId}> has an existing registration for Topic<{RegistrationItem}>",
                request.MemberId,
                request.DisplayName,
                request.GuildId,
                existingRegistration.TopicId);
            
            return ResultOf<HttpStatusCode>.Error("Already registered", HttpStatusCode.Conflict);
        }

        if (topicItem is null || timezoneItem is null)
        {
            _logger.LogWarning("User<{MemberId}> '{Username}' on Guild<{GuildId}> used an invalid topic or timezone parameter for Topic<{RegistrationItem}>, and Timezone<{Timezone}>",
                request.MemberId,
                request.DisplayName,
                request.GuildId,
                request.TopicTitle,
                request.Timezone);
            
            return ResultOf<HttpStatusCode>.Error("Invalid timezone or topic provided");
        }

        #endregion
        
        // does the user already exist in our system?
        var existingUser =
            await _context.Users.FirstOrDefaultAsync(x => x.DiscordUserId == request.MemberId, cancellationToken);

        if (existingUser is null)
        {
            _context.Users.Add(new()
            {
                Email = "changeme",
                UserName = request.DisplayName,
                NormalizedUserName = request.DisplayName,
                DiscordUserId = request.MemberId,
                DiscordDisplayName = request.DisplayName
            });
        }
        
        Registration item = new()
        {
            DiscordGuildId = request.GuildId,
            DiscordUserId = request.MemberId,
            TopicId = topicItem.Id,
            TimezoneId = timezoneItem.Id,
            ExperienceLevel = request.ExperienceLevel,
            IsSolo = request.IsSolo
        };

        _context.CodeJamRegistrations.Add(item);
        await _context.SaveChangesAsync(cancellationToken);
        
        return ResultOf<HttpStatusCode>.Success(HttpStatusCode.Created);
    }

    /// <summary>
    /// Confirm registration to a code jam
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<ResultOf<HttpStatusCode>> ConfirmRegistration(ConfirmRegistrationRequest request,
        CancellationToken cancellationToken = default)
    {
        #region Validation

        var validationResult = Validator.Validate(request);

        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors);
            _logger.LogWarning("Invalid data was given to '{Name}': {Errors}", "ConfirmRegistration", errors);
            
            return ResultOf<HttpStatusCode>.Error(errors);
        }

        var now = DateTime.Now;

        var existing = await (
            from registration in _context.CodeJamRegistrations
            join topic in _context.CodeJamTopics
                on registration.TopicId equals topic.Id
            where
                topic.IsActive &&
                //within registration timeframe
                now >= topic.RegistrationStartOn &&
                now <= topic.RegistrationEndOn &&
                registration.AbandonedOn == null &&

                registration.DiscordGuildId == request.GuildId &&
                registration.DiscordUserId == request.MemberId &&
                topic.Title == request.Topic
            select registration).FirstOrDefaultAsync(cancellationToken);

        if (existing is null)
        {
            _logger.LogWarning(
                "Member<{MemberId}> on Guild<{GuildId}> - could not locate registration request for an active topic",
                request.MemberId, request.GuildId);
            return ResultOf<HttpStatusCode>.NotFound("Unable to locate registration for an active topic");
        }
        
        #endregion

        existing.ConfirmedOn = DateTime.Now;
        existing.ConfirmationValue = request.Confirm;

        await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Member<{MemberId}> on Guild<{GuildId}> confirmed with: {Attendance}",
            request.MemberId, request.GuildId, request.Confirm ? "Yes" : "No");
        
        return ResultOf<HttpStatusCode>.Success(HttpStatusCode.OK);
    }

    /// <summary>
    /// Process a user's request to abandon a code jam
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<ResultOf<HttpStatusCode>> Abandon(AbandonRequest request,
        CancellationToken cancellationToken = default)
    {
        #region validation

        var validationResult = Validator.Validate(request);

        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors);
            _logger.LogWarning("Invalid data was provided for '{Name}': {Errors}", "Abandon", errors);
            return ResultOf<HttpStatusCode>.Error(errors);
        }

        var existing = await (from registration in _context.CodeJamRegistrations
            join topic in _context.CodeJamTopics
                on registration.TopicId equals topic.Id
            where registration.DiscordGuildId == request.GuildId &&
                  registration.DiscordUserId == request.MemberId &&
                  registration.AbandonedOn == null &&
                  topic.Title == request.Topic
            select new{Registration = registration, Topic = topic}).FirstOrDefaultAsync(cancellationToken);

        if (existing is null)
        {
            _logger.LogWarning("Member<{MemberId}> '{Username}' was unable to find a registration item on guild {GuildId}",
                request.MemberId,
                request.DisplayName,
                request.GuildId);
            
            return ResultOf<HttpStatusCode>.NotFound("Unable to locate registration");
        }

        #endregion
        
        _logger.LogWarning("Member<{MemberId}> '{Username}' on Guild<{GuildId}> is withdrawing from codejam '{Topic}'",
            request.MemberId,
            request.DisplayName,
            request.GuildId,
            request.Topic);
        
        existing.Registration.AbandonedOn = DateTime.Now;
        
        // If this jam has a withdraw point type assigned, deduct the appropriate points
        if (existing.Topic.PointType_WithdrawId is not null)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.DiscordUserId == request.MemberId,
                cancellationToken);

            var point = await _context.CodeJamPointTypes.FirstOrDefaultAsync(
                x => x.Id == existing.Topic.PointType_WithdrawId, cancellationToken);

            if (user != null && point != null)
            {
                user.Points += point.Amount;
                _logger.LogWarning("{Username}: Deducting '{Points}' points for withdrawing",
                    request.DisplayName,
                    Math.Abs(point.Amount));
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return ResultOf<HttpStatusCode>.Success(HttpStatusCode.OK);
    }
}