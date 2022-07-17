using Data.Challenges;
using Microsoft.Extensions.DependencyInjection;

namespace DiscordHub;

public static class Extensions
{
    public static IServiceCollection AddDiscordInteractionHub(this IServiceCollection services)
    {
        services.AddSingleton<InteractionHub>();
        return services;
    }
    
    /// <summary>
    /// <p>
    ///     Verifies <paramref name="text"/> begins with <paramref name="prefix"/> then outputs the last
    ///     bit of ID as <paramref name="output"/>
    /// </p>
    /// <p>
    ///     Expects the format to be "prefix_output"
    /// </p>
    /// </summary>
    /// <param name="text"></param>
    /// <param name="prefix"></param>
    /// <param name="output"></param>
    /// <returns></returns>
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

    /// <summary>
    /// <p>Verifies <paramref name="text"/> begins with <paramref name="prefix"/></p>
    /// <p>Extract challenge information from a 'custom id'</p>
    /// <p>Expects ID to be "prefix_language_id"</p>
    /// <list type="number">
    /// <item>language</item>
    /// <item>challenge id</item>
    /// </list>
    /// </summary>
    /// <param name="text"></param>
    /// <param name="prefix"></param>
    /// <param name="output"></param>
    /// <returns></returns>
    public static bool ExtractChallengeInfo(this string text, string prefix,
        out (int Id, ProgrammingLanguage Language)? output)
    {
        if (!text.StartsWith(prefix))
        {
            output = null;
            return false;
        }

        var split = text.Split('_');
        int.TryParse(split[^1], out var id);
        Enum.TryParse(split[1], out ProgrammingLanguage language);
        output = new(id, language);
        return id > 0;
    }
}