using System.Text;
using CodeJam.Interfaces;
using Data.CodeJam;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CodeJam.Events;

public record TeamWorkflowCreate(Team TeamInfo, List<UserRegistrationRecord> Members, Topic Topic) : IRequest<TeamWorkflowCreate>;
public record TeamWorkflowCreateResponse(ulong? TeamRoleId, ulong? TeamChannelId);

public class TeamWorkflowCreateHandler : IRequestHandler<TeamWorkflowCreate, TeamWorkflowCreate>
{
    private readonly IDiscordService _discord;
    private readonly ILogger<TeamWorkflowCreateHandler> _logger;
    
    public TeamWorkflowCreateHandler(IDiscordService discord, ILogger<TeamWorkflowCreateHandler> logger)
    {
        _discord = discord;
        _logger = logger;
    }

    private string TeamDisplay(TeamWorkflowCreate team)
    {
        var sb = new StringBuilder();

        sb.AppendLine($@"
    Team: {team.TeamInfo.Name}
    Members:
        {string.Join("\n\t\t\t", team.Members.Select(x => x.User.DiscordDisplayName))}
");

        return sb.ToString();
    }
    
    public async Task<TeamWorkflowCreate?> Handle(TeamWorkflowCreate request, CancellationToken cancellationToken)
    {
        var response = await _discord.CreateTeamSpace(request);
        
        if (response is null)
        {
            _logger.LogWarning("Was unable to create a team:\n{Team}", TeamDisplay(request));
            return null;
        }

        if (response.TeamRoleId is null)
        {
            _logger.LogWarning("Was unable to create team:\n{Team}\n\nTeam Role Id was not provided",
                TeamDisplay(request));
            return null;
        }

        if (response.TeamChannelId is null)
        {
            _logger.LogWarning("Was unable to create team:\n{Team}\n\nTeam Channel Id was not provided", TeamDisplay(request));
            return null;
        }

        request.TeamInfo.TeamChannelId = response.TeamChannelId.ToString()!;
        request.TeamInfo.RoleId = response.TeamRoleId.ToString()!;

        return request;
    }
}
