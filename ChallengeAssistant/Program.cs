using ChallengeAssistant;

var hostBuilder = Host.CreateDefaultBuilder(args)
    .ConfigureWebHostDefaults(builder =>
    {
        builder.UseStartup<Startup>();
    });

var app = hostBuilder.Build();
await app.RunAsync();
    