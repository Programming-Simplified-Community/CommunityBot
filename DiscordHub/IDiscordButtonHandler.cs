using System.Net;
using Core.Validation;
using Discord.WebSocket;

namespace DiscordHub;

/// <summary>
/// Contract for handling Discord button click events
/// </summary>
public interface IDiscordButtonHandler
{
    Task<ResultOf<HttpStatusCode>> HandleButton(SocketMessageComponent component);
}