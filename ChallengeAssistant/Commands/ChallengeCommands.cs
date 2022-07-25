using System.Text;
using ChallengeAssistant.Services;
using Discord;
using Discord.Interactions;
using DiscordHub;
using Razor.Templating.Core;

namespace ChallengeAssistant.Commands;

[Group("challenge", "various challenge-based commands")]
public class ChallengeCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ILogger<ChallengeCommands> _logger;
    private readonly string _website;
    private readonly ChallengeService _service;
    
    public ChallengeCommands(ILogger<ChallengeCommands> logger, ChallengeService service, IConfiguration config)
    {
        _logger = logger;
        _service = service;
        _website = config["CodeRunner:Website"];
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

        await Context.Channel.SendMessageAsync(embed: new EmbedBuilder()
            .WithTitle("Instructions")
            .WithDescription("Due to character limitations on Discord, challenge information is now distributed through these HTML files!\n\n" +
                             "Please note... that apparently your browser may directly open them without downloading. I suspect this has to do with their content delivery system?\n\n" +
                             "Otherwise, download the HTML file and open with your favorite browser!\n\n" +
                             "The leaderboard is ranked by #points, then by #attempts, then by duration")
            .WithColor(Color.Orange)
            .WithImageUrl("https://images.unsplash.com/photo-1605379399642-870262d3d051?ixlib=rb-1.2.1&ixid=MnwxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8&auto=format&fit=crop&w=1806&q=80")
            .WithThumbnailUrl("https://img.freepik.com/free-vector/laptop-with-program-code-isometric-icon-software-development-programming-applications-dark-neon_39422-971.jpg?w=1380&t=st=1658011057~exp=1658011657~hmac=5168ecbad8636d17080e01d76bad5863e488966221d0e3dee7d4b21e9b45b868")
            .Build());
        
        var challenges = (await _service.GetAll())
            .OrderBy(x=>x.Title);

        foreach (var challenge in challenges)
        {
            // The button shall be used to create a modal linked to a specific challenge.
            var comp = new ComponentBuilder();
            var embed = new EmbedBuilder()
                .WithTitle(challenge.Title)
                .WithDescription(challenge.Description);

            int styleIndex = 0;
            foreach (var test in challenge.Tests)
            {
                comp.WithButton(new ButtonBuilder()
                    .WithCustomId(string.Format(Constants.ATTEMPT_BUTTON_NAME_FORMAT, test.Language.ToString(),
                        challenge.Id))
                    .WithLabel(test.Language.ToString())
                    .WithStyle(styleIndex switch
                    {
                        1 => ButtonStyle.Danger,
                        2 => ButtonStyle.Link,
                        3 => ButtonStyle.Secondary,
                        4 => ButtonStyle.Primary,
                        _ => ButtonStyle.Success,
                    }));
                styleIndex++;
                styleIndex %= 5;
            }

            comp.WithButton(new ButtonBuilder()
                .WithLabel("View Challenge")
                .WithStyle(ButtonStyle.Link)
                .WithUrl($"{_website}/Challenge?{challenge.QueryParameter}"));

            await Context.Channel.SendMessageAsync(
                embed: embed.Build(),
                components: comp.Build());

            await Task.Delay(TimeSpan.FromSeconds(2));
        }
        
        await DeleteOriginalResponseAsync();
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

            var sb = new StringBuilder();
            sb.AppendLine("```yml");

            sb.AppendLine(string.Format("| {0,15} | {1,15} | {2,15} |", "Username", "Points", "Attempts"));
            
            foreach (var item in top)
                sb.AppendLine(string.Format("| {0,15} | {1,15} | {2,15} |", item.Username, item.Points, item.Attempts));
            
            sb.AppendLine("```");
            
            var e = new EmbedBuilder()
                .WithTitle($"Leaderboard")
                .WithDescription(sb.ToString())
                .WithFooter($"Requested by: {Context.User.Username}")
                .WithColor(Color.Orange);

            await Context.Channel.SendMessageAsync(embed: e.Build());
            await DeleteOriginalResponseAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("Error occurred while fetching leaderboard: {Exception}", ex);
        }
    }
}