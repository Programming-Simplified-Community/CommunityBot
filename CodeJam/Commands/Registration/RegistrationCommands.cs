using System.Net;
using System.Reactive.Threading.Tasks;
using CodeJam.Commands.Autocomplete;
using CodeJam.Requests;
using CodeJam.Services;
using Discord;
using Discord.Interactions;

namespace CodeJam.Commands.Registration;

[Group("registration", "Registration based commands")]
public class RegistrationCommands : InteractionModuleBase<SocketInteractionContext>
{
    public RegistrationCommands(RegistrationService registrationService)
    {
        RegistrationService = registrationService;
    }

    public RegistrationService RegistrationService { get; set; }


    [SlashCommand("view", "See what's open for registration (will DM you)")]
    public async Task ViewOpenRegistration()
    {
        var topics = await RegistrationService.ViewRegisterableTopics();

        foreach (var topic in topics)
        {
            var contents =
                $"{topic.Description}\n\nRegistration is open between `{topic.RegistrationStartOn.ToString("g")} - {topic.RegistrationEndOn.ToString("g")}`\n" +
                $"The jam will be active between `{topic.StartDateOn.ToString("g")} - {topic.EndDateOn.ToString("g")}`";

            await Context.User.SendMessageAsync(embed: Util.Embed(topic.Title, contents, MessageType.Info).Build());
            await Task.Delay(TimeSpan.FromMilliseconds(250));
        }

        if (topics.Any())
            await RespondAsync("You should have received some info in your DM's", ephemeral: true);
        else
            await RespondAsync("Unfortunately, there are no active jams accepting applicants right now...",
                ephemeral: true);
    }

    [SlashCommand("confirm", "Confirm your participation in the current jam")]
    public async Task ConfirmParticipation(
        [Autocomplete(typeof(ConfirmJamAutoCompleteProvider))] [Summary("topic", "topic to confirm participation in")]
        string topic,
        [Summary("confirm", "Are you still down to participate?")]
        bool confirm)
    {
        var result = await RegistrationService.ConfirmRegistration(new()
        {
            Confirm = confirm,
            GuildId = Context.Guild.Id.ToString(),
            MemberId = Context.User.Id.ToString(),
            Topic = topic
        });

        switch (result.StatusCode)
        {
            case HttpStatusCode.BadRequest:
                await RespondAsync(embed:
                    Util.Embed("Error", result.Message, MessageType.Error)
                        .WithFooter(result.StatusCode.ToString()).Build(), ephemeral: true);
                break;
            case HttpStatusCode.NotFound:
                await RespondAsync(embed:
                    Util.Embed("Not Found", result.Message, MessageType.Error)
                        .WithFooter(result.StatusCode.ToString()).Build(), ephemeral: true);
                break;
            case HttpStatusCode.OK:
                var message = confirm
                    ? "Your participation has been confirmed"
                    : "We find your lack of participation disturbing...";
                await RespondAsync(embed:
                    Util.Embed("Success", message, MessageType.Success)
                        .WithFooter(result.StatusCode.ToString()).Build(), ephemeral: true);
                break;
        }
    }

    [SlashCommand("apply", "Apply to an active topic")]
    public async Task Register(
        [Autocomplete(typeof(RegisterableJamAutoCompleteProvider)),
         Summary("JamTopic", "Topic you wish to participate in")]
        string category,
        [Autocomplete(typeof(TimezoneAutoCompleteProvider)),
         Summary("Timezone", "Your timezone to participate in")]
        string timezone,
        [Autocomplete(typeof(ExperienceAutoCompleteProvider)),
         Summary("ExperienceLevel", "Years of experience")]
        string experienceLevel,
        [Summary("PreferTeam", "Do you want to be on a team?")]
        bool preferTeam)
    {

        RegistrationRequest request = new()
        {
            DisplayName = Context.User.Username,
            GuildId = Context.Guild.Id.ToString(),
            MemberId = Context.User.Id.ToString(),
            TopicTitle = category,
            Timezone = timezone,
            ExperienceLevel = Util.GetExperienceLevel(experienceLevel),
            IsSolo = !preferTeam
        };

        var response = await RegistrationService.Register(request);

        switch (response.StatusCode)
        {
            case HttpStatusCode.NotFound:
                await RespondAsync(embed:
                    Util.Embed("Not Found", response.Message, MessageType.Error)
                        .WithFooter(response.StatusCode.ToString()).Build(), ephemeral: true);
                break;
            case HttpStatusCode.Created:
            case HttpStatusCode.OK:
                await RespondAsync(embed:
                    Util.Embed("Registration",
                            $"You've successfully registered for {category}!\n" +
                            "Please note, just before the jam starts you will be asked to confirm" +
                            "your decision to participate. If you `do not` confirm you `will not` be included. This is to" +
                            "help reduce the amount of team shuffling.\n\n" +
                            "If you leave in the middle of the jam -- points will be deduced from your user profile. People who are prone" +
                            "to abandoning code-jams will end up getting grouped with together in later jams.",
                            MessageType.Success)
                        .WithFooter(response.StatusCode.ToString()).Build(), ephemeral: true);
                break;
            default:
                await RespondAsync(embed:
                    Util.Embed("Error", response.Message, MessageType.Error)
                        .WithFooter(response.StatusCode.ToString()).Build(), ephemeral: true);
                break;
        }
    }
    
    [SlashCommand("withdraw", "Leave jam")]
    public async Task Withdraw(
        [Autocomplete(typeof(AbandonJamAutoCompleteProvider)), Summary("topic", "Topic to withdraw from")]
        string topic)
    {
        AbandonRequest request = new()
        {
            GuildId = Context.Guild.Id.ToString(),
            MemberId = Context.User.Id.ToString(),
            DisplayName = Context.User.Username,
            Topic = topic
        };

        var response = await RegistrationService.Abandon(request);

        switch (response.StatusCode)
        {
            case HttpStatusCode.OK:
                await RespondAsync(embed:
                    Util.Embed("Success", $"You have successfully withdrawn from {topic}",
                        MessageType.Success).Build());
                break;
            case HttpStatusCode.NotFound:
                await RespondAsync(embed:
                    Util.Embed("Not Found", response.Message, MessageType.Error).Build(), ephemeral: true);
                break;
            default:
                await RespondAsync(embed:
                    Util.Embed("Error", response.Message, MessageType.Error).Build(), ephemeral: true);
                break;
        }
    }
}