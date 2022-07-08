using CodeJam;
using Infrastructure;
using ChallengeAssistant;

var host = Host.CreateDefaultBuilder()
    .ConfigureServices((context, services) =>
    {
        services.AddOptions();
        services.AddInfrastructure(context.Configuration);
        services.AddCodeJam(context.Configuration);
        services.AddCodeRunner(context.Configuration);
    }).Build();

try
{
    var db = host.Services.GetRequiredService<SocialDbContext>();
    await CodeJam.Util.InitializeDb(db);
    await SeedDb.Seed(db);
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex);
}

await host.RunAsync();