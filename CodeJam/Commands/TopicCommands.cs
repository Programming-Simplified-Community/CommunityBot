using System.Net;
using CodeJam.Commands.Autocomplete;
using CodeJam.Services;
using Discord;
using Discord.Interactions;

namespace CodeJam.Commands;

[Group("topic", "topic-based commands")]
public class TopicCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly TopicService _topicService;
    
    public enum DateMonth
    {
        Jan = 1,
        Feb = 2,
        Mar = 3,
        Apr = 4,
        May = 5,
        Jun = 6,
        Jul = 7,
        Aug = 8,
        Sept = 9,
        Oct = 10,
        Nov = 11,
        Dec = 12
    }

    public TopicCommands(TopicService topicService)
    {
        _topicService = topicService;
    }

    [RequireUserPermission(GuildPermission.Administrator)]
    [SlashCommand("set-reg-dates", "Set topic registration date range")]
    public async Task EditRegistrationStart(
        [Autocomplete(typeof(ModeratorJamTopicAutoCompleteProvider)), Summary("topic", "Topic to modify")]
        string topic,
        [Summary("start-month", "starting month")]
        DateMonth startMonth, 
        [Summary("start-day","starting day")]
        int startDay,
        [Summary("end-month", "ending month")]
        DateMonth endMonth,
        [Summary("end-day", "ending day")]
        int endDay)
    {
        var now = DateTime.UtcNow;
        DateTime start = new(now.Year, (int)startMonth, startDay, 0, 0, 0);
        DateTime end = new(now.Year, (int)endMonth, endDay, 23, 59, 59);

        var response = await _topicService.UpdateRegistrationDateRange(new()
        {
            StartDate = start,
            EndDate = end,
            TopicTitle = topic
        });

        switch (response.StatusCode)
        {
            case HttpStatusCode.OK:
                await RespondAsync(ephemeral: true, embed: Util
                    .Embed("Updated", "Registration dates have been updated for " + topic, MessageType.Success)
                    .WithFooter(response.StatusCode.ToString()).Build());
                break;
            case HttpStatusCode.NotFound:
                await RespondAsync(ephemeral: true,
                    embed: Util.Embed("Not Found", response.Message, MessageType.Error)
                        .WithFooter(response.StatusCode.ToString()).Build());
                break;
            default:
                await RespondAsync(ephemeral: true,
                    embed: Util.Embed("Error", response.Message, MessageType.Error)
                        .WithFooter(response.StatusCode.ToString()).Build());
                break;
                    
        }
    }
    
    [RequireUserPermission(GuildPermission.Administrator)]
    [SlashCommand("set-sub-dates", "Set topic submission date range")]
    public async Task EditSubmission(
        [Autocomplete(typeof(ModeratorJamTopicAutoCompleteProvider)), Summary("topic", "Topic to modify")]
        string topic,
        [Summary("start-month", "starting month")]
        DateMonth startMonth, 
        [Summary("start-day","starting day")]
        int startDay,
        [Summary("end-month", "ending month")]
        DateMonth endMonth,
        [Summary("end-day", "ending day")]
        int endDay)
    {
        var now = DateTime.UtcNow;
        DateTime start = new(now.Year, (int)startMonth, startDay, 0, 0, 0);
        DateTime end = new(now.Year, (int)endMonth, endDay, 23, 59, 59);

        var response = await _topicService.UpdateSubmissionDateRange(new()
        {
            StartDate = start,
            EndDate = end,
            TopicTitle = topic
        });

        switch (response.StatusCode)
        {
            case HttpStatusCode.OK:
                await RespondAsync(ephemeral: true, embed: Util
                    .Embed("Updated", "Submission dates have been updated for " + topic, MessageType.Success)
                    .WithFooter(response.StatusCode.ToString()).Build());
                break;
            case HttpStatusCode.NotFound:
                await RespondAsync(ephemeral: true,
                    embed: Util.Embed("Not Found", response.Message, MessageType.Error)
                        .WithFooter(response.StatusCode.ToString()).Build());
                break;
            default:
                await RespondAsync(ephemeral: true,
                    embed: Util.Embed("Error", response.Message, MessageType.Error)
                        .WithFooter(response.StatusCode.ToString()).Build());
                break;
                    
        }
    }
}