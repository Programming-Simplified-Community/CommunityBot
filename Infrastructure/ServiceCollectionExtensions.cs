using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<SocialDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("Default");
            options.UseMySql(connectionString!, ServerVersion.AutoDetect(connectionString),
                x => x.MigrationsAssembly("Api"));
        },ServiceLifetime.Singleton);

        return services;
    }
}