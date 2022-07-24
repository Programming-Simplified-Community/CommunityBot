
using Dummy;
using Grpc.Net.Client;

namespace BotService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly string _url;
    
    public Worker(ILogger<Worker> logger, IConfiguration config)
    {
        _logger = logger;
        _url = config["Services:ChallengeService"];
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var channel = GrpcChannel.ForAddress(_url);
        var client = new DummyService.DummyServiceClient(channel);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            var reply = await client.SayHelloAsync(new HelloRequest
            {
                Name = "Worker thing"
            });
            
            _logger.LogInformation("Greeting: {Message} | {Time}", reply.Message, DateTime.Now);
            
            await Task.Delay(1000, stoppingToken);
        }
    }
}