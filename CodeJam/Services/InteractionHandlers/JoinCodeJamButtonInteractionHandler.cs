using System.Net;
using Core.Validation;
using Discord;
using Discord.WebSocket;
using DiscordHub;

namespace CodeJam.Services.InteractionHandlers;

[DiscordInteractionHandlerName("Join Code Jam", Constants.JOIN_CODE_JAM_BUTTON_PREFIX)]
public class JoinCodeJamButtonInteractionHandler : IDiscordButtonHandler
{
    private readonly ILogger<JoinCodeJamButtonInteractionHandler> _logger;
    private readonly DiscordSocketClient _client;
    
    private IRole? _cachedCodeJamRole;
    private ITextChannel? _cachedCodeJamGeneralTextChannel;
    private readonly ulong _guildId, _roleId, _codeJamGeneralId;
    private readonly string _codeJamRoleName;

    public JoinCodeJamButtonInteractionHandler(ILogger<JoinCodeJamButtonInteractionHandler> logger, 
        DiscordSocketClient client, 
        IConfiguration config)
    {
        _logger = logger;
        _client = client;
        _guildId = config.GetValue<ulong>("CodeJamBot:PrimaryGuildId");
        _roleId = config.GetValue<ulong>("CodeJamBot:CodeJamRoleId");
        _codeJamGeneralId = config.GetValue<ulong>("CodeJamBot:CodeJamGeneralId");
        _codeJamRoleName = config["CodeJamBot:CodeJamRoleName"];
    }

    public async Task<ResultOf<HttpStatusCode>> HandleButton(SocketMessageComponent component)
    {
        try
        {
            var parts = component.Data.CustomId.Split('_');

            // Make sure this is the original user who is responding... not some other user
            if (parts[^1] != component.User.Id.ToString())
            {
                await component.RespondAsync(embed: Util
                    .Embed("Oops", "This is not your message to interact with!", MessageType.Warning)
                    .Build(), ephemeral: true);
                _logger.LogWarning("{Username} attempted to interact with user id's message {id}",
                    component.User.Username, parts[^1]);
                return ResultOf<HttpStatusCode>.Success(HttpStatusCode.OK);
            }

            if (_cachedCodeJamRole is null)
                _cachedCodeJamRole = _client.GetGuild(_guildId)
                    .GetRole(_roleId);

            if (_cachedCodeJamRole is null)
            {
                _logger.LogError("Was unable to locate {RoleName} - unable to add it to{Username}",
                    _codeJamRoleName,
                    component.User.Username);
                await component.RespondAsync(embed: Util.Embed("Error", "An error occurred while processing your request",
                    MessageType.Error).Build(),
                    ephemeral: true);
                return ResultOf<HttpStatusCode>.Success(HttpStatusCode.OK);
            }

            var guildUser = _client.GetGuild(_guildId).GetUser(component.User.Id);
            await guildUser.AddRoleAsync(_cachedCodeJamRole);

            _logger.LogInformation("Added {Username} to {RoleName}", component.User.Username, _cachedCodeJamRole.Name);
            
            // Apparently the local cache is only built after calling stuff like this? -- interesting
            if(_cachedCodeJamGeneralTextChannel is null)
                _cachedCodeJamGeneralTextChannel =_client.GetGuild(_guildId)
                    .GetTextChannel(_codeJamGeneralId);

            if (_cachedCodeJamGeneralTextChannel is null)
            {
                _logger.LogWarning("Was unable to locate {ChannelName} for {Username}", "cj-general", component.User.Username);
                await component.RespondAsync(embed: Util.Embed("Error", "An error occurred while processing your request",
                    MessageType.Error).Build(),
                    ephemeral: true);
                return ResultOf<HttpStatusCode>.Success(HttpStatusCode.OK);
            }

            await _cachedCodeJamGeneralTextChannel.SendMessageAsync(
                $"Hey, {component.User.Mention}! This is where you can use the slash command `/registration apply`!");

            await component.Message.DeleteAsync();
            await component.RespondAsync($"Welcome! Please head over to {_cachedCodeJamGeneralTextChannel.Mention}!", ephemeral: true);
        }
        catch (Exception ex)
        {
            _logger.LogError("Was unable to process user request for button {CustomId}, for user {Username}\nError: {Error}",
                component.Data.CustomId,
                component.User.Username,
                ex);
            
            return ResultOf<HttpStatusCode>.Error("Unable to process request");
        }
        
        return ResultOf<HttpStatusCode>.Success(HttpStatusCode.OK);
    }
}