﻿using ChallengeAssistant.Services;
using CodeJam.Events;
using CodeJam.Interfaces;
using Data.CodeJam;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CodeJam.Services;

public class CodeJamBot : BackgroundService, IDiscordService
{
    private readonly Settings _settings;
    public static IServiceProvider Provider;
    private readonly DiscordSocketClient _client;
    private readonly ILogger<CodeJamBot> _logger;
    private readonly IConfiguration _config;
    private readonly InteractionService _commands;
    private readonly SocialDbContext _context;
    private readonly TopicService _topicService;
    private readonly ulong _welcomeChannelId;
    private IRole? _cachedCodeJamRole;
    private ITextChannel? _cachedCodeJamGeneralTextChannel;
    
    public CodeJamBot(IConfiguration config, 
        ILogger<CodeJamBot> logger, 
        IServiceProvider serviceProvider, 
        IHostApplicationLifetime applicationLifetime,
        IOptions<Settings> settings,
        DiscordSocketClient client,
        InteractionService commands, SocialDbContext context, 
        TopicService topicService)
    {
        _config = config;
        _logger = logger;
        _client = client;
        _commands = commands;
        _context = context;
        _topicService = topicService;
        _settings = settings.Value;
        _guildId = config.GetValue<ulong>("CodeJamBot:PrimaryGuild");
        _welcomeChannelId = config.GetValue<ulong>("CodeJamBot:WelcomeChannelId");

        Provider = serviceProvider.CreateScope().ServiceProvider;

        _client.Log += LogAsync;
        _client.Ready += ReadyAsync;
        _client.UserJoined += ClientOnUserJoined;
        _client.ButtonExecuted += ClientOnButtonExecuted;
    }

    private async Task ClientOnButtonExecuted(SocketMessageComponent arg)
    {
        if (string.IsNullOrEmpty(arg.Data.CustomId))
            return;

        try
        {
            var parts = arg.Data.CustomId.Split('_');

            var wantsToJoin = parts.First().ToLower() == "cjj";

            // Make sure this is the original user who is responding... not some other user
            if (parts[^1] != arg.User.Id.ToString())
            {
                await arg.RespondAsync(embed: Util
                    .Embed("Oops", "This is not your message to interact with!", MessageType.Warning)
                    .Build(), ephemeral: true);
                _logger.LogWarning("{Username} attempted to interact with user id's message {id}",
                    arg.User.Username, parts[^1]);
                return;
            }

            // Clean up the message because it's not desired
            if (!wantsToJoin)
            {
                await arg.Message.DeleteAsync();
                await arg.RespondAsync(
                    embed: Util.Embed("No worries!", "Please introduce yourself to the community!", MessageType.Info)
                        .Build(),
                    ephemeral: true);
                return;
            }

            if (_cachedCodeJamRole is null)
                _cachedCodeJamRole = _client.GetGuild(_guildId)
                    .GetRole(_config.GetValue<ulong>("CodeJamBot:CodeJamRoleId"));

            if (_cachedCodeJamRole is null)
            {
                _logger.LogError("Was unable to locate {RoleName} - unable to add it to{Username}",
                    _config["CodeJamBot:CodeJamRoleName"],
                    arg.User.Username);
                await arg.RespondAsync(embed: Util.Embed("Error", "An error occurred while processing your request",
                    MessageType.Error).Build(),
                    ephemeral: true);
                return;
            }

            var guildUser = _client.GetGuild(_guildId).GetUser(arg.User.Id);
            await guildUser.AddRoleAsync(_cachedCodeJamRole);

            _logger.LogInformation("Added {Username} to {RoleName}", arg.User.Username, _cachedCodeJamRole.Name);
            
            // Apparently the local cache is only built after calling stuff like this? -- interesting
            if(_cachedCodeJamGeneralTextChannel is null)
                _cachedCodeJamGeneralTextChannel =_client.GetGuild(_guildId)
                    .GetTextChannel(_config.GetValue<ulong>("CodeJamBot:CodeJamGeneralId"));

            if (_cachedCodeJamGeneralTextChannel is null)
            {
                _logger.LogWarning("Was unable to locate {ChannelName} for {Username}", "cj-general", arg.User.Username);
                await arg.RespondAsync(embed: Util.Embed("Error", "An error occurred while processing your request",
                    MessageType.Error).Build(),
                    ephemeral: true);
                return;
            }

            await _cachedCodeJamGeneralTextChannel.SendMessageAsync(
                $"Hey, {arg.User.Mention}! This is where you can use the slash command `/registration apply`!");

            await arg.Message.DeleteAsync();
            await arg.RespondAsync($"Welcome! Please head over to {_cachedCodeJamGeneralTextChannel.Mention}!", ephemeral: true);
        }
        catch (Exception ex)
        {
            _logger.LogError("Was unable to process user request for button {CustomId}, for user {Username}\nError: {Error}",
                arg.Data.CustomId,
                arg.User.Username,
                ex);
        }
    }

     private async Task CreateWelcomeMessage(string username, string mention, string id)
    {
        // Need to ensure we have our welcome channel readily available.
        var welcomeChannel = _client.GetGuild(_guildId).GetChannel(_welcomeChannelId);
        
        if (welcomeChannel is null)
        {
            _logger.LogWarning(
                "Was unable to welcome {Username} to the server. Unable to locate welcome channel with Id: {Id}",
                username,
                _welcomeChannelId);
            return;
        }
        
        // We have to limit ourselves on how many components we show due to discord limits. 
        var registerableTopics = await _topicService.GetRegisterableTopics();

        // If there are no topics to register for we'll skip this entire thing
        if(!registerableTopics.Any())
            return;
        
        var message =
            $"Greetings, {mention}! If you're here to participate in one of the following code-jams:\n" +
            "```yml\n" +
            $"{string.Join("\n", registerableTopics.Select(x=>x.Title))}\n" +
            "```\n" +
            "Please click the join button below!";
        
        var compBuilder = new ComponentBuilder()
            .WithButton(new ButtonBuilder()
                .WithCustomId($"cjj_{id}")
                .WithStyle(ButtonStyle.Primary)
                .WithLabel("Join"))
            .WithButton(new ButtonBuilder()
                .WithCustomId($"cji_{id}")
                .WithLabel("No Thanks")
                .WithStyle(ButtonStyle.Danger));
        
        var textChannel = welcomeChannel as SocketTextChannel;
        
        await textChannel!.SendMessageAsync(embed: new EmbedBuilder()
                .WithTitle("Welcome")
                .WithDescription(message)
                .WithColor(Color.Blue)
                .Build(),
            components: compBuilder.Build());

        _logger.LogInformation("Welcomed {Username} to the server", username);
    }

    private async Task ClientOnUserJoined(SocketGuildUser arg)
    {
        await Task.Delay(TimeSpan.FromSeconds(30));
        await CreateWelcomeMessage(arg.Username, arg.Mention, arg.Id.ToString());
    }

    private bool _isReady;
    private readonly ulong _guildId;
    private static Dictionary<ulong, IGuildChannel> s_CategoryChannels = new();

    private async Task<string?> CreateUserMentionFor(ulong guildId, ulong memberId) =>
        (await _client.GetUserAsync(memberId))?.Mention ?? string.Empty;

    public async Task<TeamWorkflowCreateResponse?> CreateTeamSpace(TeamWorkflowCreate request)
    {
        IGuildChannel? categoryChannel = null;
        #region Lookup Cache
        if (!s_CategoryChannels.Any())
        {
            var channels = await Cache.ChannelCache.GetDict(_guildId, _client.GetGuild(_guildId));
            var roles = await Cache.RoleCache.GetDict(_guildId, _client.GetGuild(_guildId));

            var pair = channels.FirstOrDefault(x => x.Value.Name == _settings.CodeJamCategoryName);
            var cjRoleInfo = roles.FirstOrDefault(x => x.Value.Name == _settings.CodeJamRoleName);

            if (pair.Key <= 0)
            {
                try
                {
                    categoryChannel =
                        await _client.GetGuild(_guildId).CreateCategoryChannelAsync(_settings.CodeJamCategoryName);
                    
                     await categoryChannel.AddPermissionOverwriteAsync(categoryChannel.Guild.EveryoneRole,  OverwritePermissions.DenyAll(categoryChannel));
                }
                catch (Exception ex)
                {
                    _logger.LogError("Was unable to either create category channel or modify permissions. {Exception}",
                        ex);

                    return new TeamWorkflowCreateResponse(null, null);
                }

                Cache.ChannelCache.Set(_guildId, categoryChannel.Id, categoryChannel);
            }
            else categoryChannel = pair.Value;

            var cjRole = cjRoleInfo.Value;

            if (cjRoleInfo.Key <= 0)
            {
                cjRole = await _client.GetGuild(_guildId)
                    .CreateRoleAsync(_settings.CodeJamRoleName, _settings.GuildPermissions);
                
                Cache.RoleCache.Set(_guildId,cjRole.Id,cjRole);
            }
            
            await categoryChannel.AddPermissionOverwriteAsync(cjRole, _settings.GeneralPermissions);

            if (!s_CategoryChannels.ContainsKey(_guildId))
                s_CategoryChannels.Add(_guildId, categoryChannel);
        }
        #endregion

        ITextChannel? teamChannel;
        IRole? teamRole;

        try
        {
            teamChannel = await _client.GetGuild(_guildId).CreateTextChannelAsync(request.TeamInfo.Name,
                options => options.CategoryId = categoryChannel.Id);
            teamRole = await _client.GetGuild(_guildId)
                .CreateRoleAsync(request.TeamInfo.Name, _settings.GuildPermissions, isMentionable: true);

            await teamChannel.AddPermissionOverwriteAsync(teamRole, _settings.GeneralPermissions);

            var guild = _client.GetGuild(_guildId);
            foreach (var member in request.Members)
                try
                {
                    var user = await _client.GetUserAsync(ulong.Parse(member.User.DiscordUserId));
                    await guild.GetUser(user.Id).AddRoleAsync(teamRole);
                }
                catch (Exception ex)
                {
                    _logger.LogError("Was unable to give team role to {username} for topic {topic}\n{Exception}", member.User.DiscordDisplayName, request.Topic.Title, ex);
                }
        }
        catch (Exception ex)
        {
            _logger.LogError("Was unable to create the team channel or role: {Exception}", ex);
            return new TeamWorkflowCreateResponse(null, null);
        }

        request.TeamInfo.RoleId = teamRole.Id.ToString();
        request.TeamInfo.TeamChannelId = teamChannel.Id.ToString();

        Cache.ChannelCache.Set(_guildId, teamChannel.Id, teamChannel);
        Cache.RoleCache.Set(_guildId, teamRole.Id, teamRole);

        var mentionTasks =
            request.Members.Select(x => CreateUserMentionFor(_guildId, ulong.Parse(x.Registration.DiscordUserId))).ToArray();

        await Task.WhenAll(mentionTasks);
        
        // Send a message into the channel
        var mentions = string.Join(", ", mentionTasks.Select(x => x.Result));

        var requirements = await _context.CodeJamRequirements.Where(x => x.TopicId == request.Topic.Id).ToListAsync();
        string require = string.Join("\n", requirements.Select(x => x.Information));
        string acceptance = string.Join("\n", requirements.Select(x => x.AcceptanceCriteria));
        
        var welcomeMessage = await teamChannel.SendMessageAsync(embed: 
            Util.Embed("Greetings",
                    $"Welcome {mentions}!\n" +
                    $"```\n{request.Topic.Description}```\n\n" +
                    $"Requirements:\n```md\n{require}\n```\n" +
                    $"```md\n{acceptance}\n```",
                    MessageType.Info)
                .WithFooter("Good luck")
                .Build());

        // Since this should contain the requirements, having it pinned should help for future reference
        await welcomeMessage.PinAsync();
        
        return new TeamWorkflowCreateResponse(teamRole.Id, teamChannel.Id);
    }

    public async Task<bool> SendConfirmationMessage(Registration registration, string message)
    {
        var validGuild = ulong.TryParse(registration.DiscordGuildId, out var guildId);
        var validMemberId = ulong.TryParse(registration.DiscordUserId, out var memberId);

        if (!validGuild || !validMemberId)
        {
            _logger.LogError("Unable to send confirmation message. Invalid Guild <{Guild}> or Member <{Member}> Ids",
                registration.DiscordGuildId,
                registration.DiscordUserId);

            return false;
        }

        var user = _client.GetGuild(guildId).GetUser(memberId);

        if (user is null)
        {
            _logger.LogError("Was unable to request confirmation for <{User}> for Topic {Topic}",
                registration.DiscordUserId, registration.TopicId);
            return false;
        }

        try
        {
            await user.SendMessageAsync(embed: Util.Embed("Code Jam", message, MessageType.Warning).Build());
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Something went wrong when sending confirmation message to: {User}\n{Exception}", user.Username, ex);
            return false;
        }

        return true;
    }

    public ValueTask<bool> IsBotHealthy()
    {
        return ValueTask.FromResult(_isReady);
    }

    private Task LogAsync(LogMessage log)
    {
        switch (log.Severity)
        {
            case LogSeverity.Critical:
                _logger.LogCritical(log.Message);
                break;
            case LogSeverity.Debug:
                _logger.LogDebug(log.Message);
                break;
            case LogSeverity.Error:
                _logger.LogError(log.Message);
                break;
            case LogSeverity.Info:
                _logger.LogInformation(log.Message);
                break;
            case LogSeverity.Warning:
                _logger.LogWarning(log.Message);
                break;
        }

        return Task.CompletedTask;
    }
    private async Task ReadyAsync()
    {
        try
        {
            await _commands.AddModulesAsync(typeof(Settings).Assembly, Provider);
            await _commands.AddModulesAsync(typeof(ICodeRunner).Assembly, Provider);
            
            await _commands.RegisterCommandsToGuildAsync(_guildId);
            _client.InteractionCreated += async interaction =>
            {
                var scope = Provider.CreateScope();
                var ctx = new SocketInteractionContext(_client, interaction);
                await _commands.ExecuteCommandAsync(ctx, scope.ServiceProvider);
            };

            _isReady = true;
        }
        catch (Exception ex)
        {
            _logger.LogError("An error occurred while initializing bot: {Error}", ex);
        }
    }
    
    /// <summary>
    /// Apparently, we need to download users for a certain use-case we have....
    /// </summary>
    /// <param name="guildId"></param>
    /// <returns></returns>
    public async Task<bool> DownloadUsers(ulong guildId)
    {
        try
        {
            await _client.GetGuild(guildId).DownloadUsersAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("Was unable to download users from guild {Guild}\n{Exception}", guildId, ex);
            return false;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting CodeJamBot...");

        _client.Disconnected += exception =>
        {
            _logger.LogError("Error. Bot is disconnecting: {Exception}", exception);
            return Task.CompletedTask;
        };
        
        _logger.LogWarning(_config.GetValue<string>("CodeJamBot:Discord:Token"));
        await _client.LoginAsync(TokenType.Bot, _config.GetValue<string>("CodeJamBot:Discord:Token"));
        
        await _client.StartAsync();
        
        while (!stoppingToken.IsCancellationRequested)
            await Task.Delay(-1, stoppingToken);
        
        _logger.LogWarning("CodeJamBot shutting down...");
    }
}