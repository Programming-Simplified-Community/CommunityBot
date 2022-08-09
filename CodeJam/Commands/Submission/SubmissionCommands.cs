using System.Net;
using System.Text;
using CodeJam.Commands.Autocomplete;
using CodeJam.Requests;
using CodeJam.Services;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;

namespace CodeJam.Commands.Submission;

[Group("submission", "Submission-based commands")]
public class SubmissionCommands : InteractionModuleBase<SocketInteractionContext>
{
    public SubmissionService Service { get; set; }
    public ILogger<SubmissionCommands> Logger { get; set; }

    [SlashCommand("list", "output list of submissions")]
    [RequireUserPermission(GuildPermission.BanMembers)]
    public async Task ListSubmissions()
    {
        try
        {
            var results = await Service.GetSubmissions();

            var compiled = string.Join("\n", results.Select(x => $"- {x.Name}: `{x.Topic}` {x.Submission.GithubRepo}"));

            await RespondAsync(ephemeral: true,
                embed: Util.Embed("Submissions", $"```md\n{compiled}\n```", MessageType.Info).Build());
        }
        catch (Exception ex)
        {
            await RespondAsync(ephemeral: true, embed: Util.Embed("Error", ex.Message, MessageType.Error).Build());
            Logger.LogError("Error getting submissions: {Exception}", ex);
        }
    }
    
    [SlashCommand("submit", "submit your project!")]
    public async Task SubmitProject(
        [Autocomplete(typeof(ActiveJamAutoCompleteProvider))] 
        [Summary("topic", "topic to submit to")]
        string topic,
        [Summary("repo-url", "public github repo link")]
        string repo)
    {
        SubmissionRequest request = new()
        {
            DisplayName = Context.User.Username,
            Topic = topic,
            GithubRepo = repo,
            GuildId = Context.Guild.Id.ToString(),
            MemberId = Context.User.Id.ToString()
        };

        var result = await Service.Submit(request);
        
        switch (result.Result)
        {
            case HttpStatusCode.Created:
            case HttpStatusCode.OK:
                await RespondAsync(embed:
                    Util.Embed("Submission", $"Your submission has been entered for '{topic}' at '{repo}'",
                        MessageType.Success)
                        .WithFooter(result.StatusCode.ToString()).Build(), ephemeral: true);
                break;
            
            default:
                await RespondAsync(embed:
                    Util.Embed("Error", result.Message, MessageType.Error)
                        .WithFooter(result.StatusCode.ToString()).Build(), ephemeral: true);
                break;
        }

    }
}