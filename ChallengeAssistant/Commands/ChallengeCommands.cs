using System.Net;
using System.Text;
using ChallengeAssistant.Commands.AutoComplete;
using ChallengeAssistant.Services;
using Data.Challenges;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
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

    [SlashCommand("display-challenge", "can manually re-display a challenge")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public async Task DisplayChallenge([Autocomplete(typeof(ProgrammingChallengeAutoCompleteProvider))] string title)
    {
        var challenge = await _service.FindChallenge(title);

        if (challenge is null)
        {
            await RespondAsync(ephemeral: true,
                embed: new EmbedBuilder()
                    .WithColor(Color.Red)
                    .WithTitle("Oops")
                    .WithDescription("Was unable to locate challenge with title: " + title)
                    .Build());
            return;
        }

        try
        {

            // The button shall be used to create a modal linked to a specific challenge.
            var comp = new ComponentBuilder();
            var embed = new EmbedBuilder()
                .WithTitle(challenge.Title)
                .WithDescription(challenge.Description);

            var styleIndex = 0;
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

            await Context.Channel.SendMessageAsync(embed: embed.Build(),
                components: comp.Build());
        }
        catch (Exception ex)
        {
            _logger.LogError("An error occurred while trying to display challenge: {Title}. {Error}",
                title,
                ex);
            await RespondAsync(ephemeral: true,
                embed: new EmbedBuilder()
                    .WithTitle("Error")
                    .WithDescription(ex.Message)
                    .WithColor(Color.Red)
                    .Build());
        }
    }
    
    [SlashCommand("compare", "compare your submissions with others!")]
    public async Task Compare(ProgrammingLanguage language)
    {
        _logger.LogInformation("{Usernamme} requested to compare their submissions with others for {Language}",
            Context.User.Username,
            language);
        
        if (Context.Channel is IThreadChannel)
        {
            await RespondAsync(ephemeral: true, embed: new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle("Oops")
                .WithDescription("This command will generate a thread. You cannot have thread-ception going on here")
                .Build());
            return;
        }
        
        var result = await _service.CompareMySubmissions(Context.User.Id.ToString(), language);

        if (result is null)
        {
            await RespondAsync(ephemeral: true, 
                embed: new EmbedBuilder()
                    .WithTitle("Oops")
                    .WithColor(Color.Red)
                    .WithDescription("Appears you don't have any submissions").Build());
            return;
        }

        try
        {
            var channel = Context.Channel as SocketTextChannel;
            
            if (channel is null)
            {
                await RespondAsync(ephemeral: true,
                    embed: new EmbedBuilder()
                        .WithColor(Color.Red)
                        .WithTitle("OOPS")
                        .WithDescription("Ran into an issue creating thread").Build());
                return;
            }

            var threadName = $"{Context.User.Username}-Rabbit-Hole";
            var thread = channel.Threads.FirstOrDefault(x => x.Name == threadName) ?? await channel.CreateThreadAsync(threadName);
            var htmlResult = await RazorTemplateEngine.RenderAsync("~/Views/Shared/SubmissionCompare.cshtml", result);
            await RespondAsync(ephemeral: true, text: "Generating notes...");
            using var reportStream = new MemoryStream();
            var reportHtmlBytes = Encoding.ASCII.GetBytes(htmlResult);
            reportStream.Write(reportHtmlBytes);
            await reportStream.FlushAsync();
            reportStream.Position = 0;
            await thread.SendFileAsync(reportStream,
                $"{Context.User.Username}-comparison-{DateTime.UtcNow:g}.html",
                $"{Context.User.Mention}, dive into the rabbit hole! Awaaayyyy!!!!!");
        }
        catch (Exception ex)
        {
            _logger.LogError("Something went wrong while rendering submission compare page. {Error}", ex);
            await RespondAsync(ephemeral: true,
                embed: new EmbedBuilder()
                    .WithTitle("Oops")
                    .WithColor(Color.Red)
                    .WithDescription("Something went wrong while rendering page.").Build());
        }
        
    }

    [RequireUserPermission(GuildPermission.Administrator)]
    [SlashCommand("update-attempts", "update a user's number of attempts")]
    public async Task UpdateUserAttempts(ProgrammingLanguage language, 
        IUser user, 
        int reduceAttemptsBy, 
        [Autocomplete(typeof(ProgrammingChallengeAutoCompleteProvider))] string title)
    {
        _logger.LogInformation("{User} is attempting to update {OtherUser}'s number of attempts for {Challenge} by {Number}",
            Context.User.Username,
            user.Username,
            title,
            reduceAttemptsBy
            );
        
        var response = await _service.ReduceUserAttemptsBy(user.Id.ToString(),
            Math.Abs(reduceAttemptsBy),
            title,
            language);

        var embed = new EmbedBuilder()
            .WithTitle("Challenge Helper");

        switch (response)
        {
            case HttpStatusCode.OK:
                await RespondAsync(ephemeral: true,
                    embed: embed.WithDescription("Successfully updated attempts for " + Context.User.Username)
                        .WithColor(Color.Green)
                        .WithFooter(response.ToString()).Build());
                break;
            case HttpStatusCode.BadRequest:
                await RespondAsync(ephemeral: true,
                    embed: embed
                        .WithDescription(
                            "Error locating things... are you sure this user submitted something for that?")
                        .WithColor(Color.Red)
                        .WithFooter(response.ToString()).Build());
                break;
            default:
                await RespondAsync(ephemeral: true,
                    embed: embed.WithDescription("Yeah IDK")
                        .WithFooter(response.ToString())
                        .WithColor(Color.Purple)
                        .Build());
                break;
        }
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

        var comps = new ComponentBuilder().WithButton(new ButtonBuilder()
            .WithStyle(ButtonStyle.Link)
            .WithUrl("https://programming-simplified-community.github.io/Challenger/")
            .WithLabel("Website"));
        
        await Context.Channel.SendMessageAsync(embed: new EmbedBuilder()
            .WithTitle("Instructions")
            .WithDescription("Due to character limitations on Discord, challenge information is hosted on github pages!\n\n" +
                             "The leaderboard is ranked by #points, then by #attempts, then by duration")
            .WithColor(Color.Orange)
            .WithImageUrl("https://images.unsplash.com/photo-1605379399642-870262d3d051?ixlib=rb-1.2.1&ixid=MnwxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8&auto=format&fit=crop&w=1806&q=80")
            .WithThumbnailUrl("https://img.freepik.com/free-vector/laptop-with-program-code-isometric-icon-software-development-programming-applications-dark-neon_39422-971.jpg?w=1380&t=st=1658011057~exp=1658011657~hmac=5168ecbad8636d17080e01d76bad5863e488966221d0e3dee7d4b21e9b45b868")
            .Build(),
            components: comps.Build());
        
        var challenges = (await _service.GetAll())
            .OrderBy(x=>x.Title);

        foreach (var challenge in challenges)
        {
            // The button shall be used to create a modal linked to a specific challenge.
            var comp = new ComponentBuilder();
            var embed = new EmbedBuilder()
                .WithTitle(challenge.Title)
                .WithDescription(challenge.Description);

            var styleIndex = 0;
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
            var html = await RazorTemplateEngine.RenderAsync("~/Views/Shared/Leaderboard.cshtml", entries);
            using var reportStream = new MemoryStream();
            var reportHtmlBytes = Encoding.ASCII.GetBytes(html);
            reportStream.Write(reportHtmlBytes);
            await reportStream.FlushAsync();
            reportStream.Position = 0;
            await Context.Channel.SendFileAsync(reportStream,
                $"leaderboard.html",
                $"{Context.User.Mention}, here's the current leaderboard!!!");
            await DeleteOriginalResponseAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("Error occurred while fetching leaderboard: {Exception}", ex);
        }
    }

    [RequireUserPermission(GuildPermission.Administrator)]
    [SlashCommand("rerun-submission", "rerun a user's submission")]
    public async Task RerunUserSubmission(IUser user,
        [Autocomplete(typeof(ProgrammingChallengeAutoCompleteProvider))]
        string title,
        ProgrammingLanguage language)
    {
        var response = await _service.RerunUserSubmission(user.Id.ToString(),
            title,
            language);
        
        var embed = new EmbedBuilder()
            .WithTitle("Challenge Assistant")
            .WithFooter(response.ToString());

        switch (response)
        {
            case HttpStatusCode.OK:
                await RespondAsync(ephemeral: true,
                    embed: embed.WithDescription("User's challenge is queued")
                        .WithColor(Color.Green)
                        .Build());
                break;
            default:
                await RespondAsync(ephemeral: true,
                    embed: embed.WithDescription("Something went wrong")
                        .WithColor(Color.Red).Build());
                break;
        }
    }
}