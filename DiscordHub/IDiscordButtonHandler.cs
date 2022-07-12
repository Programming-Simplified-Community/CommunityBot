using System.Net;
using Core.Validation;
using Discord.WebSocket;

namespace DiscordHub;

public interface IDiscordButtonHandler
{
    Task<ResultOf<HttpStatusCode>> HandleButton(SocketMessageComponent component);
}