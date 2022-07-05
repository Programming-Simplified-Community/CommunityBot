using CodeJam;
using Infrastructure;

var host = Host.CreateDefaultBuilder()
    .ConfigureServices((context, services) =>
    {
        services.AddOptions();
        services.AddInfrastructure(context.Configuration);
        services.AddCodeJam(context.Configuration);
    }).Build();

try
{
    var db = host.Services.GetRequiredService<SocialDbContext>();
    await Util.InitializeDb(db);
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex);
}

await host.RunAsync();