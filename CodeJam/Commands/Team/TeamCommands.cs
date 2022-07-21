﻿using System.Text;
using CodeJam.Commands.Autocomplete;
using CodeJam.Services;
using Discord;
using Discord.Interactions;
using DiscordHub;
using Razor.Templating.Core;

namespace CodeJam.Commands.Team;

[Group("team", "team based commands")]
public class TeamCommands : InteractionModuleBase<SocketInteractionContext>
{
    public ILogger<TeamCommands> Logger { get; set; }
    public TeamNameService TeamNameService { get; set; }
    public TeamCreationService TeamCreationService { get; set; }

    private readonly Emoji _thumbsUp = new ("👍");
    private readonly Emoji _thumbsDown = new ("👎");

    [RequireUserPermission(GuildPermission.BanMembers)]
    [SlashCommand("view-teams", "view the current team breakdowns and submissions")]
    public async Task ViewTeams()
    {
        await RespondAsync("Processing", ephemeral: true);
        var view = await TeamCreationService.GenerateTeamSheet();
        var html = await RazorTemplateEngine.RenderAsync("~/Views/Shared/CodeJamTeams.cshtml", view);
        using var reportStream = new MemoryStream();
        var reportHtmlBytes = Encoding.ASCII.GetBytes(html);
        reportStream.Write(reportHtmlBytes);
        await reportStream.FlushAsync();
        reportStream.Position = 0;
        await Context.Channel.SendFileAsync(reportStream, "teams.html", "Team");
        await DeleteOriginalResponseAsync();
    }
    
    [RequireUserPermission(GuildPermission.ManageMessages)]
    [SlashCommand("change-name", "update team name")]
    public async Task UpdateTeamName(string teamName)
    {
        await RespondAsync(text: "processing");
        
        var item = await TeamNameService.StartVote(Context.User.Id.ToString(), teamName);
        
        if (item is null)
        {
            await ModifyOriginalResponseAsync(x=>
            {
                x.Content = "Was unable to process request at this time.";
            });
            
            Logger.LogWarning("Unable to start team request process. {TeamName}, {User}", teamName,
                Context.User.Username);
            return;
        }
        
        var embed = new EmbedBuilder()
            .WithTitle("Vote!")
            .WithDescription($"Do you want to change our team name to: `{teamName}`?\n\n" +
                             $"Majority rules! Please vote using thumbs up/down to cast your vote")
            .Build();
        
        var compBuilder = new ComponentBuilder();
        compBuilder.WithButton(new ButtonBuilder()
            .WithCustomId(string.Format(Constants.TEAM_NAME_VOTE_YES_BUTTON_NAME_FORMAT, item.TeamId, item.Id))
            .WithStyle(ButtonStyle.Success)
            .WithEmote(_thumbsUp));
        compBuilder.WithButton(new ButtonBuilder()
            .WithStyle(ButtonStyle.Danger)
            .WithEmote(_thumbsDown)
            .WithCustomId(string.Format(Constants.TEAM_NAME_VOTE_NO_BUTTON_NAME_FORMAT, item.TeamId, item.Id)));

        await ModifyOriginalResponseAsync(x =>
        {
            x.Embed = embed;
            x.Components = compBuilder.Build();
            x.Content = string.Empty;
        });
    }
}