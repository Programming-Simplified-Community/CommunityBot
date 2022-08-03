using System.Net;
using Core.Validation;
using Discord.WebSocket;

namespace DiscordHub;

/// <summary>
/// Contract for handling Discord Modal submissions
/// </summary>
public interface IDiscordModalHandler
{
    Task<ResultOf<HttpStatusCode>> HandleModal(SocketModal modal);
}