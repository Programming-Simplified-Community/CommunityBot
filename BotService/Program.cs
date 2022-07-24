using BotService;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((host, services) =>
    {
        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();