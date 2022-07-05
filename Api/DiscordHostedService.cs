using Discord;
using Discord.Addons.Hosting;
using Discord.WebSocket;

namespace Api;

public class DiscordHostedService<T> : IHostedService where T : BaseSocketClient
{
    private readonly ILogger<DiscordHostedService<T>> _logger;
    private readonly T _client;
    private readonly DiscordHostConfiguration _config;
    
    public DiscordHostedService(ILogger<DiscordHostedService<T>> logger, IConfiguration config, T client)
    {
        _logger = logger;
        _config = new()
        {
            Token = config.GetValue<string>("CodeJamBot:Discord:Token"),
            SocketConfig = new()
            {
                AlwaysDownloadUsers = true
            }
        };
        _client = client;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Discord.NET hosted service starting...");
        try
        {
            await _client.LoginAsync(TokenType.Bot, _config.Token, true).WaitAsync(cancellationToken);
            await _client.StartAsync().WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning("Startup has been aborted, exiting...");
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Discord.NET hosted service is stopping");
        try
        {
            await _client.StopAsync().WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning("Discord.NET client could not be stopped within the given timeout and may have permanently deadlocked1");
        }
    }
}