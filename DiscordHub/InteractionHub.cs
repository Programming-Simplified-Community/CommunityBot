using System.Net;
using System.Reflection;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DiscordHub;

internal record ButtonHandlerInfo(string Name, IDiscordButtonHandler Handler);

internal record ModalHandlerInfo(string Name, IDiscordModalHandler Handler);

/// <summary>
/// Centralized location for redirecting user interactions to appropriate handlers.
/// </summary>
public class InteractionHub
{
    private readonly ILogger<InteractionHub> _logger;
    private readonly IServiceProvider _serviceProvider;

    // These dictionaries store the PREFIX of a command, separated by an underscore
    private Dictionary<string, ButtonHandlerInfo> _buttonHandlers = new();
    private Dictionary<string, ModalHandlerInfo> _modalHandlers = new();
    
    
    public InteractionHub(ILogger<InteractionHub> logger, DiscordSocketClient client, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;

        client.ModalSubmitted += ProcessModal;
        client.ButtonExecuted += ProcessButton;

        Initialize();
    }

    void Initialize()
    {
        /*
            All handlers should be added into our DI pipeline. That means we can grab a list of 
            said interface to find all handlers in our application.
            
            This avoids the "I didn't want that registered" mistake
         */
        var buttonHandlers = _serviceProvider.GetServices<IDiscordButtonHandler>();
        var modalHandlers = _serviceProvider.GetServices<IDiscordModalHandler>();
        
        // We want to cache each handler by the "prefix" they are meant to handle.
        // this prefix is what drives routing 
        foreach (var handler in buttonHandlers)
        {
            var attribute = handler.GetType().GetCustomAttribute<DiscordInteractionHandlerNameAttribute>();
            if (attribute is null)
            {
                _logger.LogWarning("{TypeName} is a button interaction handler that does not have the {Attribute}",
                    handler.GetType().Name,
                    nameof(DiscordInteractionHandlerNameAttribute));
                continue;
            }

            if (!_buttonHandlers.ContainsKey(attribute.Prefix))
                _buttonHandlers.Add(attribute.Prefix, new(attribute.Name, handler));
            else
            {
                var existing = _buttonHandlers[attribute.Prefix];
                _logger.LogError(
                    "Unable to register {TypeName} as a button handler. {Existing} already handles {Prefix}",
                    handler.GetType().Name,
                    existing.Handler.GetType().Name,
                    attribute.Prefix);
            }
        }

        foreach (var handler in modalHandlers)
        {
            var attribute = handler.GetType().GetCustomAttribute<DiscordInteractionHandlerNameAttribute>();
            if (attribute is null)
            {
                _logger.LogWarning("{TypeName} is a modal interaction handler that does not have the {Attribute}",
                    handler.GetType().Name,
                    nameof(DiscordInteractionHandlerNameAttribute));
                continue;
            }

            if (!_modalHandlers.ContainsKey(attribute.Prefix))
                _modalHandlers.Add(attribute.Prefix, new(attribute.Name, handler));
            else
            {
                var existing = _modalHandlers[attribute.Prefix];
                _logger.LogError(
                    "Unable to register {TypeName} as a modal handler. {Existing} already handles {Prefix}",
                    handler.GetType().Name,
                    existing.Handler.GetType().Name,
                    attribute.Prefix);
            }
        }
    }

    private async Task ProcessButton(SocketMessageComponent arg)
    {
        // Our hub requires custom IDs.
        if (string.IsNullOrEmpty(arg.Data.CustomId))
        {
            await arg.RespondAsync(ephemeral: true, text: "This component you interacted with has an invalid ID");
            return;
        }

        try
        {
            var prefix = arg.Data.CustomId.Split('_').First();
            
            if (!_buttonHandlers.ContainsKey(prefix))
            {
                await arg.RespondAsync("There was no interaction handler setup for this component", ephemeral: true);
                return;
            }

            var response = await _buttonHandlers[prefix].Handler.HandleButton(arg);
            
            var code = (int)response.StatusCode;
            var success = code is >= 200 and <= 299;
            
            if (!success || !arg.HasResponded)
            {
                await arg.RespondAsync(embed: new EmbedBuilder()
                        .WithTitle("Error")
                        .WithDescription(response.Message ?? "Error occurred while processing interaction")
                        .WithColor(Color.Red)
                        .WithFooter(code.ToString())
                        .Build(),
                    ephemeral: true);
                _logger.LogError("Error occurred while processing {Prefix} with handler {Name}.\n{ErrorMessage}\nStatus Code: {Code}",
                    prefix,
                    _buttonHandlers[prefix].Name,
                    response.Message,
                    code);
                return;
            }

            _logger.LogInformation("Handler: {HandlerName} processed {Prefix}",
                _buttonHandlers[prefix].Name,
                prefix);
        }
        catch (Exception ex)
        {
            _logger.LogError("An error happened while processing a button interaction: {Error}", ex);
        }
    }

    private async Task ProcessModal(SocketModal modal)
    {
        if (string.IsNullOrEmpty(modal.Data.CustomId))
        {
            await modal.RespondAsync(ephemeral: true, text: "This component you interacted with has an invalid ID");
            return;
        }

        try
        {
            var prefix = modal.Data.CustomId.Split('_').First();

            if (!_modalHandlers.ContainsKey(prefix))
            {
                await modal.RespondAsync("There was no interaction handler setup for this component", ephemeral: true);
                return;
            }

            var response = await _modalHandlers[prefix].Handler.HandleModal(modal);

            var code = (int)response.StatusCode;
            var success = code is >= 200 and <= 299;

            if (!success || !modal.HasResponded)
            {
                await modal.RespondAsync(embed: new EmbedBuilder()
                        .WithTitle("Error")
                        .WithDescription(response.Message ?? "Error occurred while processing interaction")
                        .WithColor(Color.Red)
                        .WithFooter(code.ToString())
                        .Build(),
                    ephemeral: true);
                _logger.LogError("Error occurred while processing {Prefix} with handler {Name}.\n{ErrorMessage}\nStatus Code: {Code}",
                    prefix,
                    _buttonHandlers[prefix].Name,
                    response.Message,
                    code);
                return;
            }
            
            _logger.LogInformation("Handler: {HandlerName} processed {Prefix}",
                _modalHandlers[prefix].Name,
                prefix);
        }
        catch (Exception ex)
        {
            _logger.LogError("An error happened while processing a modal interaction: {Error}", ex);
        }
    }
}