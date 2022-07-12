using CodeJam;
using Infrastructure;
using ChallengeAssistant;

var host = Host.CreateDefaultBuilder()
    .ConfigureAppConfiguration(config =>
    {
        config.AddEnvironmentVariables();
        #if DEBUG
        config.AddUserSecrets<Program>();
        #endif
    })
    .ConfigureLogging((context, logging) =>
    {
        var config = context.Configuration.GetSection("Logging");
        logging.AddConfiguration(config);
        logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
    })
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