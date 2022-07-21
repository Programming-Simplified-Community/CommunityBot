using CodeJam.Events;
using CodeJam.Interfaces;
using CodeJam.ViewModels;
using Data;
using Data.CodeJam;
using Discord.WebSocket;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Namotion.Reflection;

namespace CodeJam.Services;

public class TeamCreationService : ITeamCreationService
{
    private readonly IDiscordService _discord;
    private readonly IMediator _mediator;
    private readonly ILogger<TeamCreationService> _logger;
    private readonly SocialDbContext _context;
    private readonly DiscordSocketClient _client;
    public TeamCreationService(DiscordSocketClient client, IDiscordService discord, ILogger<TeamCreationService> logger, SocialDbContext context, IMediator mediator)
    {
        _client = client;
        _discord = discord;
        _logger = logger;
        _context = context;
        _mediator = mediator;
    }

    private async Task<string> GetAvatar(string userId)
    {
        var response = await _client.GetUserAsync(ulong.Parse(userId!));
        return response.GetAvatarUrl();
    }
    
    public async Task<IList<TeamView>> GenerateTeamSheet()
    {
        var results = await (from registration in _context.CodeJamRegistrations
            join user in _context.Users
                on registration.DiscordUserId equals user.DiscordUserId
            join team in _context.CodeJamTeams
                on registration.TeamId equals team.Id
            where registration.AbandonedOn == null
            select new
            {
                User = user,
                TeamId = team.Id,
                TeamName = team.Name
            }).ToListAsync();
        
        var members = results.GroupBy(x=>x.TeamId)
            .ToDictionary(x=>x.Key, x=>x.ToList());
        
        var teamIds = members.Keys.ToHashSet();
        
#pragma warning disable CS8714
        // Null check is already performed in the where clause thus making it safe
        // to use TeamID as a key
        var submissions = await _context.CodeJamSubmissions
            .Where(x => x.TeamId != null && teamIds.Contains(x.TeamId.Value))
            .ToDictionaryAsync(x=>x.TeamId, x=>x);
#pragma warning restore CS8714

        Dictionary<int, Dictionary<string, string>> userCache = new();
        Dictionary<int, string> teamNames = new();
        
        foreach (var user in results)
        {
            if (!teamNames.ContainsKey(user.TeamId))
            {
                teamNames.Add(user.TeamId, user.TeamName);
                userCache.Add(user.TeamId, new());
            }
            
            var avatar = await GetAvatar(user.User.DiscordUserId);
            if(!userCache[user.TeamId].ContainsKey(user.User.UserName))
                userCache[user.TeamId].Add(user.User.UserName, avatar);
        }

        return userCache.Select(x => new TeamView(teamNames[x.Key],
                submissions.ContainsKey(x.Key) 
                    ? submissions[x.Key] 
                    : null,
                userCache[x.Key]))
            .ToList();
    }

    /// <summary>
    /// Retrieve registered users by their timezones
    /// </summary>
    /// <param name="topicId"></param>
    /// <returns></returns>
    public async Task<Dictionary<Timezone, List<UserRegistrationRecord>>> GetRegisteredUsersByTimezone(int topicId)
    {
        var results = await (from registration in _context.CodeJamRegistrations
                             join user in _context.Users
                                 on registration.DiscordUserId equals user.DiscordUserId
                             where registration.TopicId == topicId
                             select new UserRegistrationRecord(user, registration))
            .ToListAsync();
        
        var timezones = await _context.CodeJamTimezones.ToDictionaryAsync(x=>x.Id, x=>x);
        
        return results
            .GroupBy(x => x.Registration.TimezoneId)
            .ToDictionary(x => timezones[x.Key], x => x.ToList());
    }

    /// <summary>
    /// Helper method for generating an abbreviated name
    /// </summary>
    /// <param name="text"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    string GetAbbreviationFor(string text, int length)
    {
        text = text.Replace("&", string.Empty);
        length = Math.Min(text.Length, length);

        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        return text[..length];
    }
    
    /// <summary>
    /// Calculate teams for a given topic and timezone based on the users in said area
    /// </summary>
    /// <param name="topic"></param>
    /// <param name="timezone"></param>
    /// <param name="userPool"></param>
    /// <returns></returns>
    public async Task<bool> CalculateTeamsFor(Topic topic, Timezone timezone, List<UserRegistrationRecord> userPool)
    {
        var topicAbbreviation =
            topic.Title.Contains(' ')
                ? string.Join('-', topic.Title.Split(' ').Select(x => GetAbbreviationFor(x, 2)))
                : topic.Title[..2];

        var numOfTeams = NumOfTeams(userPool.Count);
        var tz = GetAbbreviationFor(timezone.Name, 2);
        
        _logger.LogWarning("Timezone {Tz} will have {Teams} teams. Pool of {MemberCount} members",
            tz,
            numOfTeams,
            userPool.Count);

        Dictionary<Team, List<UserRegistrationRecord>> teams = new();

        int teamIndex;
        for (teamIndex = 0; teamIndex <= numOfTeams; teamIndex++)
        {
            var teamName = $"{tz}-{topicAbbreviation}-{teamIndex + 1}";
            _logger.LogInformation("Creating Team: {TeamName}", teamName);

            Team currentTeam = new()
            {
                Name = teamName,
                TopicId = topic.Id
            };
            
            teams.Add(currentTeam, new());
        }

        // Going through members who want to be on a team
        teamIndex = 0;
        foreach (var memberInfo in userPool
                     .Where(x=>!x.Registration.IsSolo)
                     .OrderByDescending(x => x.Registration.ExperienceLevel))
        {
            teams.ElementAt(teamIndex).Value.Add(memberInfo);
            teamIndex++;

            if (teamIndex >= teams.Count)
                teamIndex = 0;
        }
        
        // at this point in time we need to actually 'create' our discord server stuff
        foreach (var info in teams)
        {
            if(!info.Value.Any())
                continue;
            
            try
            {
                var response = await _mediator.Send(new TeamWorkflowCreate(info.Key, info.Value, topic));
                // should now be populated with the discord role/channel ID

                _context.CodeJamTeams.Add(response.TeamInfo);
                await _context.SaveChangesAsync(); // this will populate our newly created team
                
                _context.CodeJamTeamMembers.AddRange(response.Members.Select(x=>new TeamMember
                {
                    TeamId = response.TeamInfo.Id,
                    MemberId = x.User.DiscordUserId!
                }));

                foreach (var member in info.Value)
                {
                    member.Registration.TeamId = response.TeamInfo.Id;
                    _context.CodeJamRegistrations.Update(member.Registration);
                }
                
                await _context.SaveChangesAsync();
                _logger.LogInformation("Team: {Team}, Discord presence created", info.Key.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError("Was unable to establish the team on Discord. {Error}", ex);
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// How many teams should there be based on <paramref name="memberCount"/>
    /// </summary>
    /// <param name="memberCount"></param>
    /// <returns></returns>
    private static int NumOfTeams(int memberCount)
    {
        var half = (int)Math.Ceiling((double)memberCount / 2);
        var tens = (int)Math.Ceiling((double)memberCount / 11);
        var teamSize = (int)Math.Max(Math.Ceiling((double)half / tens), 3);
        return (int)Math.Max(Math.Floor((double)memberCount / teamSize), 1);
    }

    public Task<bool> GenerateTeams()
    {
        throw new NotImplementedException();
    }
}