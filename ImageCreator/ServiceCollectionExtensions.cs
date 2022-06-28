using ImageCreator.Interfaces;
using ImageCreator.Services;

namespace ImageCreator;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddImageServices(this IServiceCollection services)
    {
        services.AddSingleton<IImageService, UnsplashImageService>();
        return services;
    }
}