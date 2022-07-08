using System.Text.RegularExpressions;
using Data.Challenges;

namespace ChallengeAssistant;

public static class Util
{
    /// <summary>
    /// Regex which will grab the last segment of URL
    /// </summary>
    internal static Regex LastSegmentOfUrlRegex = new("[^/]+(?=/$|$)");

    public static string ToDiscordChallengeId(this ProgrammingChallenge challenge)
        => $"challenge_{challenge.Id}";

    public static string ToDiscordModalId(this ProgrammingChallenge challenge)
        => $"modal_{challenge.Id}";

    public static int ExtractDiscordModalChallengeId(this string id)
    {
        int.TryParse(id[6..], out var challengeId);
        return challengeId;
    }
    
    public static int ExtractDiscordButtonChallengeId(this string id)
    {
        int.TryParse(id[10..], out var challengeId);
        return challengeId;
    }
    
    public static async Task DeleteDir(string dirPath)
    {
        await PS.Execute($"Remove-Item -Recurse -Force \"{dirPath}\"");
    }
}