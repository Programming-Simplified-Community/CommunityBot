using System.Net;
using Core.Validation;
using Data.CodeJam;
using Discord.WebSocket;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CodeJam.Services;

public class TeamNameService
{
    private readonly ILogger<TeamNameService> _logger;
    private readonly SocialDbContext _context;
    private readonly DiscordSocketClient _client;
    private readonly ulong _guildId;
    
    public TeamNameService(ILogger<TeamNameService> logger, SocialDbContext context, DiscordSocketClient client,
        IOptions<Settings> settings)
    {
        _logger = logger;
        _context = context;
        _client = client;
        _guildId = settings.Value.PrimaryGuildId;
    }

    /// <summary>
    /// Determines if the number of votes on a team exceeds the threshold needed to change team name
    /// </summary>
    /// <param name="team"></param>
    /// <param name="info"></param>
    /// <param name="targetValue">The value we're querying against</param>
    /// <returns></returns>
    private bool MeetsThreshold(Team team, TeamNameVote info, bool targetValue=true)
    {
        var threshold = Math.Max((int)Math.Ceiling((double)team.Members.Count / 2), 2);
        
        if (team.Members.Count == 1) threshold = 1;
        
        var count = !targetValue 
            ? info.Votes.Count(x => !x.Value) 
            : info.Votes.Count(x => x.Value);

        return count >= threshold;
    }
    
    /// <summary>
    /// Start voting process. Automatically retrieves a user's team
    /// </summary>
    /// <param name="discordUserId"></param>
    /// <param name="proposedName"></param>
    /// <returns></returns>
    public async Task<TeamNameVote?> StartVote(string discordUserId, string proposedName)
    {
        var query = await (from registration in _context.CodeJamRegistrations
            join team in _context.CodeJamTeams
                on registration.TeamId equals team.Id
            where registration.DiscordUserId == discordUserId
            select team)
            .FirstOrDefaultAsync();
        
        if(query is null)
            return null;

        var item = new TeamNameVote
        {
            TeamId = query.Id,
            ProposedName = proposedName,
            ProposedByUserId = discordUserId
        };

        _context.TeamNameVotes.Add(item);
        await _context.SaveChangesAsync();
        return item;
    }

    public async Task<ResultOf<HttpStatusCode>> AddOrUpdateUserVote(string discordUserId, int teamId, int teamVoteId, bool value)
    {
        var user = await _context.GetUserFromDiscordId(discordUserId);

        if (user is null)
            return ResultOf<HttpStatusCode>.NotFound("Could not locate user");

        var team = await _context.CodeJamTeams
            .Include(x=>x.Members)
            .FirstOrDefaultAsync(x => x.Id == teamId);

        if (team is null)
            return ResultOf<HttpStatusCode>.NotFound("Could not locate team");

        var voteItem = await _context.TeamNameVotes.Include(x => x.Votes)
            .FirstOrDefaultAsync(x => x.Id == teamVoteId);

        if (voteItem is null)
            return ResultOf<HttpStatusCode>.NotFound("Unable to find team vote");

        var existingVote = voteItem.Votes.FirstOrDefault(x => x.UserId == user.Id);

        if (existingVote is not null)
            existingVote.Value = value;
        else
        {
            var vote = new UserTeamNameVote
            {
                UserId = user.Id,
                Value = value,
                TeamNameVoteId = teamVoteId
            };
            await _context.SaveChangesAsync();

            voteItem.Votes.Add(vote);
        }
        
        await _context.SaveChangesAsync();
        
        if (MeetsThreshold(team, voteItem))
        {
            var old = team.Name;
            // update our database first so if the later parts fail, at lest the database is updated
            team.Name = voteItem.ProposedName;

            // Free database space since this is no longer needed
            _context.UserTeamNameVotes.RemoveRange(voteItem.Votes);
            _context.TeamNameVotes.Remove(voteItem);
            
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated {Team}'s name in database to {Name}", old, team.Name);
            
            var teamRole = _client.GetGuild(_guildId).GetRole(ulong.Parse(team.RoleId));

            if (teamRole is null)
            {
                _logger.LogWarning("Was unable to update {Team}'s role name to {Name}, could not locate role {RoleId}",
                    team.Name,
                    voteItem.ProposedName,
                    team.RoleId);
                return ResultOf<HttpStatusCode>.Error("Server error");
            }

            await teamRole.ModifyAsync(x =>
            {
                x.Name = voteItem.ProposedName;
            });

            _logger.LogInformation("Updated {Team}'s role name to {Name}", team.Name, voteItem.ProposedName);
            
            var channel = _client.GetGuild(_guildId).GetTextChannel(ulong.Parse(team.TeamChannelId));
        
            if (channel is null)
            {
                _logger.LogWarning(
                    "Was unable to update {Team}'s channel name to {Name}, could not locate channel {ChannelId}",
                    team.Name,
                    voteItem.ProposedName,
                    team.TeamChannelId);
                return ResultOf<HttpStatusCode>.Error("Server error");
            }

            await channel.ModifyAsync(x =>
            {
                x.Name = voteItem.ProposedName;
            });
            
            _logger.LogInformation("Updated {Team}'s channel name to {Name}", team.Name, voteItem.ProposedName);
            return ResultOf<HttpStatusCode>.Success(HttpStatusCode.Created);
        }

        if (MeetsThreshold(team, voteItem, false))
        {
            _context.UserTeamNameVotes.RemoveRange(voteItem.Votes);
            _context.TeamNameVotes.Remove(voteItem);
            await _context.SaveChangesAsync();
            
            // get it... not acceptable... haha!
            return ResultOf<HttpStatusCode>.Success(HttpStatusCode.NotAcceptable, voteItem.ProposedName);
        }
        
        return ResultOf<HttpStatusCode>.Success(HttpStatusCode.OK);
    }
}