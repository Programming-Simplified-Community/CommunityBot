using System.Net;
using Core.Validation;
using Discord.WebSocket;

namespace DiscordHub;

public interface IDiscordModalHandler
{
    Task<ResultOf<HttpStatusCode>> HandleModal(SocketModal modal);
}