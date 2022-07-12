using ChallengeAssistant.Commands.AutoComplete;
using ChallengeAssistant.Services;
using Discord;
using Discord.Interactions;
using DiscordHub;
using Microsoft.Extensions.Logging;

namespace ChallengeAssistant.Commands;

[Group("challenge", "various challenge-based commands")]
public class ChallengeCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ILogger<ChallengeCommands> _logger;
    private readonly ChallengeService _service;
    
    public ChallengeCommands(ILogger<ChallengeCommands> logger, ChallengeService service)
    {
        _logger = logger;
        _service = service;
    }

    [RequireUserPermission(GuildPermission.Administrator)]
    [SlashCommand("generate", "create interactable challenges")]
    public async Task GenerateInChannel()
    {
        await RespondAsync(text: "Processing...", ephemeral: true); // This can take a bit so respond to the user to let them know things are popping
        
        foreach (var message in await Context.Channel.GetMessagesAsync().FlattenAsync())
        {
            await Context.Channel.DeleteMessageAsync(message);
            await Task.Delay(TimeSpan.FromSeconds(2));
        }
        
        var challenges = (await _service.GetAll())
            .OrderBy(x=>x.Title);

        foreach (var challenge in challenges)
        {
            var e = new EmbedBuilder()
                .WithTitle(challenge.Title)
                .WithDescription(challenge.Question)
                .WithColor(Color.Teal);
                
            // The button shall be used to create a modal linked to a specific challenge.
            var comp = new ComponentBuilder()
                .WithButton(new ButtonBuilder()
                    .WithLabel("Attempt")
                    .WithCustomId(string.Format(Constants.ATTEMPT_BUTTON_NAME_FORMAT, challenge.Id))
                    .WithStyle(ButtonStyle.Success));

            await Context.Channel.SendMessageAsync(embed: e.Build(), components: comp.Build());
            await Task.Delay(TimeSpan.FromSeconds(2));
        }
        
        await ModifyOriginalResponseAsync(x =>
        {
            x.Content = "Complete";
        });
    }

    [SlashCommand("leaderboard-for", "view specific leaderboard")]
    public async Task ViewLeaderboardFor(
        [Autocomplete(typeof(ProgrammingChallengeAutoCompleteProvider))]
        string challenge)
    {
        var entries = await _service.ViewLeaderboardFor(challenge);

        if (!entries.Any())
        {
            await RespondAsync("Appears no one has tried this challenge yet!", ephemeral: true);
            return;
        }

        var top = entries.Take(20);

        var e = new EmbedBuilder()
            .WithTitle($"Leaderboard | {challenge}")
            .WithFooter($"Requested by: {Context.User.Username}")
            .WithColor(Color.Orange);

        foreach (var item in top)
            e.AddField(item.Username, item.Report.Points, inline: true);

        await RespondAsync(embed: e.Build());
    }

    [SlashCommand("leaderboard", "view leaderboard")]
    public async Task ViewLeaderBoard()
    {
        await RespondAsync(text: "Fetching leaderboard...");
        
        var entries = await _service.ViewLeaderboard();

        if (!entries.Any())
        {
            await ModifyOriginalResponseAsync(x=>x.Content= "Appears no one has tried this challenge yet!");
            return;
        }

        try
        {
            var top = entries.Take(20);

            var e = new EmbedBuilder()
                .WithTitle($"Leaderboard")
                .WithFooter($"Requested by: {Context.User.Username}")
                .WithColor(Color.Orange);

            foreach (var item in top)
                e.AddField(item.Username, item.Report.Points, inline: true);

            await ModifyOriginalResponseAsync(message =>
            {
                message.Content = string.Empty;
                message.Embed = e.Build();
            });
        }
        catch (Exception ex)
        {
            await ModifyOriginalResponseAsync(message => message.Content = "Failed to fetch");
            _logger.LogError("Error occurred while fetching leaderboard: {Exception}", ex);
        }
    }
    
    [SlashCommand("view", "view available challenges")]
    public async Task ViewChallenges([Autocomplete(typeof(ProgrammingChallengeAutoCompleteProvider))] string title)
    {
        var item = await _service.FindChallenge(title);

        if (item is null)
        {
            await RespondAsync($"Was unable to locate challenge: {title}", ephemeral: true);
            return;
        }

        _logger.LogInformation("{User} requested to look at challenge {Title}", Context.User.Username, title);
        
        await RespondAsync(embed: new EmbedBuilder()
                .WithTitle(item.Title)
                .WithDescription(item.Question)
                .WithFooter(string.Join(", ", item.Tests.Select(x => x.Language)))
                .Build(),
            ephemeral: true);
    }
}