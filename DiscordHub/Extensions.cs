using Microsoft.Extensions.DependencyInjection;

namespace DiscordHub;

public static class Extensions
{
    public static IServiceCollection AddDiscordInteractionHub(this IServiceCollection services)
    {
        services.AddSingleton<InteractionHub>();
        return services;
    }

    public static bool ExtractFrom(this string text, string prefix, out string output)
    {
        if (!text.StartsWith(prefix))
        {
            output = string.Empty;
            return false;
        }

        output = text[(prefix.Length + 1)..];
        return true;
    }
}