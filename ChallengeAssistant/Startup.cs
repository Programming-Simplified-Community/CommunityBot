namespace ChallengeAssistant;

public class Startup
{
    private IConfiguration _configuration;
    
    public Startup(IConfiguration config, IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddGrpc();
        services.AddCodeRunner(_configuration);
    }

    public void Configure(IApplicationBuilder app, IHostEnvironment env)
    {
        app.UseRouting();
        app.UseCodeRunner();
    }
}