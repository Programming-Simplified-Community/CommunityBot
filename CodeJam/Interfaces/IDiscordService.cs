using CodeJam.Events;
using Data.CodeJam;
using Microsoft.Extensions.Hosting;

namespace CodeJam.Interfaces;

public interface IDiscordService : IHostedService
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    Task<TeamWorkflowCreateResponse?> CreateTeamSpace(TeamWorkflowCreate request);

    /// <summary>
    /// Download cache of users from guild
    /// </summary>
    /// <param name="guild"></param>
    /// <returns></returns>
    Task<bool>DownloadUsers(ulong guild);

    /// <summary>
    /// Send confirmation message to the applicant.
    /// </summary>
    /// <param name="registration"></param>
    /// <param name="message"></param>
    /// <returns>
    ///     <para><c>True</c> If message delivered / sent without error</para>
    ///     <para><c>False</c> If the message was unable to be delivered (for whatever reason)</para>
    /// </returns>
    Task<bool> SendConfirmationMessage(Registration registration, string message);
    
    /// <summary>
    /// Heartbeat, to determine if this discord bot is alive / available for tasking
    /// </summary>
    /// <returns></returns>
    ValueTask<bool> IsBotHealthy();
}