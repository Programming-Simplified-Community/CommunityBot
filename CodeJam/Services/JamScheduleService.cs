using CodeJam.Interfaces;
using Data;
using Data.CodeJam;
using Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CodeJam.Services;

public class JamScheduleService : BackgroundService
{
    private readonly SocialDbContext _context;
    private readonly ILogger<JamScheduleService> _logger;
    private readonly TeamCreationService _teamCreationService;
    private readonly IDiscordService _discord;
    private readonly IConfiguration _config;
    private bool DownloadedUsers = false;

    /// <summary>
    /// <para>String Format</para>
    /// <para>
    /// <list type="number">
    /// <item>Username</item>
    /// <item>Topic Title</item>
    /// <item>Hours Left</item>
    /// </list>
    /// </para>
    /// </summary>
    private readonly string _hoursLeftMessageFormat = "Greetings, {0}! This is another friendly reminder that you signed up to participate in a {1} CodeJam!" +
                                                      "We noticed you have still not confirmed your participation! " +
                                                      "There are {2} hours left until registration closes! If you " +
                                                      "do not confirm within this time you will not get placed onto a team and " +
                                                      "not participate in this jam! Please come to the server and use the " +
                                                      "`/registration confirm` command to confirm your participation!";

    /// <summary>
    /// <para>String Format</para>
    /// <para>
    /// <list type="number">
    ///     <item>Username</item>
    ///     <item>Topic Title</item>
    ///     <item>Registration date</item>
    /// </list>
    /// </para>
    /// </summary>
    private readonly string _daysLeftMessageFormat = "Greetings, {0}! This is a friendly reminder that you signed up to participate in a {1} CodeJam! " +
                                                     "Every applicant **must** confirm their participation by using the `/registration confirm` command on our server! " +
                                                     "You have until {2} to do so, otherwise you will **not** be participating!";

    private Dictionary<int, Timezone> _timezones = new();

    public JamScheduleService(TeamCreationService teamCreationService, ILogger<JamScheduleService> logger, SocialDbContext context, IDiscordService discord, IConfiguration config)
    {
        _teamCreationService = teamCreationService;
        _logger = logger;
        _context = context;
        _discord = discord;
        _config = config;
    }

    /// <summary>
    /// Checks for topics that should now be "starting"
    /// </summary>
    private async Task CheckTopicsStarting()
    {
        var now = DateTime.Now;

        var registrations = await (
            from reg in _context.CodeJamRegistrations
            join topic in _context.CodeJamTopics
                on reg.TopicId equals topic.Id
            join requirement in _context.CodeJamRequirements
                on topic.Id equals requirement.TopicId
            join user in _context.Users
                on reg.DiscordUserId equals user.DiscordUserId
            where !reg.IsSolo &&
                  reg.ConfirmationValue == true &&
                  now >= topic.StartDateOn &&
                  now <= topic.EndDateOn &&
                  reg.TeamId == null
            select new
            {
                User = user,
                Topic = topic,
                Registration = reg,
                Requirement = requirement
            }
        ).ToListAsync();

        if(!_timezones.Any())
            _timezones = await _context.CodeJamTimezones.ToDictionaryAsync(x=>x.Id, x=>x);
        
        if (!registrations.Any())
            return;
        
        _logger.LogWarning("Jam Scheduler: {template}",
            string.Join("\n", registrations.Select(x=>$"{x.User.DiscordDisplayName} -- {x.Topic.Title}")));

        Dictionary<Topic, List<UserRegistrationRecord>> map = new();
        
        foreach(var entry in registrations)
            if (map.ContainsKey(entry.Topic))
                map[entry.Topic].Add(new UserRegistrationRecord(entry.User, entry.Registration));
            else
                map.Add(entry.Topic, new(){new(entry.User, entry.Registration)});

        List<Team> createdTeams = new();

        foreach (var pair in map)
        {
            var timezoneGroups = pair.Value.GroupBy(x => x.Registration.TimezoneId);

            foreach (var timezonePair in timezoneGroups)
            {
                var response = await _teamCreationService.CalculateTeamsFor(pair.Key, _timezones[timezonePair.Key], timezonePair.ToList());

                if (!response)
                {
                    _logger.LogError("Was unable to create a team for {Topic} | {Timezone} | {Members}",
                        pair.Key.Title,
                        _timezones[timezonePair.Key].Name,
                        string.Join(", ", timezonePair.Select(x=>x.User.DiscordDisplayName)));
                }
            }
        }
    }

    /// <summary>
    /// Handle confirmation messages, send them to users who have not yet confirmed
    /// </summary>
    private async Task HandleConfirmationMessages()
    {
        var now = DateTime.Now;

        var registrations = await (
            from reg in _context.CodeJamRegistrations
            join topic in _context.CodeJamTopics
                on reg.TopicId equals topic.Id
            join user in _context.Users
                on reg.DiscordUserId equals user.DiscordUserId
            where !reg.IsSolo && reg.ConfirmationValue == null
            select new
            {
                Registration = reg,
                Topic = topic,
                User = user
            }).ToListAsync();

        try
        {
            foreach (var reg in registrations)
            {
                if (!(now >= reg.Topic.RegistrationStartOn && now <= reg.Topic.RegistrationEndOn))
                    continue;
                
                var daysLeft = (int)Math.Abs((now - reg.Topic.RegistrationEndOn).TotalDays);
                var hoursLeft = (int)Math.Abs((now - reg.Topic.RegistrationEndOn).TotalHours);
                
                if (daysLeft is > 1 and <= 2)
                    await SendDayMessage(reg.Topic, reg.Registration, reg.User, daysLeft);
                else if (hoursLeft is > 2 and < 24)
                    await SendHourMessage(reg.Topic, reg.Registration, reg.User, hoursLeft);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Was unable to handle confirmation message\n\n{Exception}. {Registrations}\n{Stack}",
                ex.Message,
                registrations != null
                            ? $"#{registrations.Count} | {string.Join(", ", registrations.Select(x=>$"{x.User.DiscordDisplayName} - {x.Topic.Title}"))}"
                            : "Error in database",
                ex.StackTrace);
        }
    }

    private async Task SendHourMessage(Topic topic, Registration registration, SocialUser user, int left)
    {
        // For hourly messages we want to make sure there is a period of no messaging...
        if (registration.ReminderSentOn.HasValue && (DateTime.Now - registration.ReminderSentOn.Value).Hours <= 6)
            return;
        
        _logger.LogInformation("Sending [Hour] Reminder to {User} for topic {Topic}", user.DiscordDisplayName, topic.Title);
        var message = string.Format(_hoursLeftMessageFormat,
            user.DiscordDisplayName,
            topic.Title,
            left);
        await _discord.SendConfirmationMessage(registration, message);

        registration.ReminderSentOn = DateTime.Now;
        await _context.SaveChangesAsync();
    }

    private async Task SendDayMessage(Topic topic, Registration registration, SocialUser user, int left)
    {
        // Need at least more than 1 day for daily one
        if (registration.ReminderSentOn.HasValue && (DateTime.Now - registration.ReminderSentOn.Value).TotalDays < 1)
            return;

        _logger.LogInformation("Sending [Day] Reminder to {User} for topic {Topic}", user.DiscordDisplayName,
            topic.Title);
        var message = string.Format(_daysLeftMessageFormat,
            user.DiscordDisplayName,
            topic.Title,
            left);

        await _discord.SendConfirmationMessage(registration, message);
        await _context.SaveChangesAsync();
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!await _discord.IsBotHealthy())
        {
            _logger.LogWarning("{Method} Waiting for bot to reach a healthy state", nameof(JamScheduleService));
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            if (!DownloadedUsers)
            {
                DownloadedUsers = await _discord.DownloadUsers(_config.GetValue<ulong>("CodeJamBot:PrimaryGuild"));

                if (!DownloadedUsers)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    continue;
                }
            }
            
            await HandleConfirmationMessages();
            await CheckTopicsStarting();
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }

        _logger.LogWarning("Jam Scheduler is shutting down...");
    }
}