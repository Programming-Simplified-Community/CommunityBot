using CodeJam.Commands.Registration;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using IResult = Discord.Interactions.IResult;

namespace CodeJam.Services;

public class CommandHandler
{
    private readonly DiscordSocketClient _client;
    private readonly ILogger<CommandHandler> _logger;
    private readonly InteractionService _commands;
    private readonly IServiceProvider _services;

    public CommandHandler(DiscordSocketClient client, ILogger<CommandHandler> logger, IServiceProvider services, InteractionService commands)
    {
        _client = client;
        _logger = logger;
        _services = services.CreateScope().ServiceProvider;
        _commands = commands;
    }

    public async Task InitializeAsync()
    {
        //await _commands.AddModulesAsync(typeof(CommandHandler).Assembly, _services);
        await _commands.AddModuleAsync(typeof(RegistrationCommands), _services);

        _client.SlashCommandExecuted += ClientOnSlashCommandExecuted;
    }

    private Task ClientOnSlashCommandExecuted(SocketSlashCommand arg)
    {
        throw new NotImplementedException();
    }

    private Task SlashCommandExecuted(SlashCommandInfo info, IInteractionContext context, IResult result)
    {
        if (!result.IsSuccess)
        {
            switch (result.Error)
            {
                case InteractionCommandError.UnmetPrecondition:
                    _logger.LogError("Slash Command {Name} has unmet precondition. Reason: {Reason}", 
                        info.Name, 
                        result.ErrorReason);
                    break;
                case InteractionCommandError.Unsuccessful:
                    _logger.LogError("Slash Command {Name} was unsuccessful because: {Reason}",
                        info.Name,
                        result.ErrorReason);
                    break;
                case InteractionCommandError.BadArgs:
                    _logger.LogError("Slash Command {Name} had bad arguments: {Reason}",
                        info.Name,
                        result.ErrorReason);
                    break;
                case InteractionCommandError.Exception:
                    _logger.LogError("Slash Command {Name} had an exception: {Reason}",
                        info.Name,
                        result.ErrorReason);
                    break;
                default:
                    _logger.LogError("Slash Command {Name}, something happened: {Reason}",
                        info.Name,
                        result.ErrorReason);
                    break;
            }
        }

        return Task.CompletedTask;
    }
}