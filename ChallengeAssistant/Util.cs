using System.Text.RegularExpressions;
using Core.Storage;
using Data.Challenges;
using Octokit;

namespace ChallengeAssistant;

public static class Util
{
    /// <summary>
    /// Regex which will grab the last segment of URL
    /// </summary>
    internal static Regex LastSegmentOfUrlRegex = new("[^/]+(?=/$|$)");

    public static void EnsureDir(string dirPath)
    {
        if (!Directory.Exists(dirPath))
            Directory.CreateDirectory(dirPath);
    }
    
    public static async Task DeleteDir(string dirPath, bool filesOnly=true)
    {
        if (filesOnly && Directory.Exists(dirPath))
        {
            var files = Directory.GetFiles(dirPath);
            foreach (var file in files)
                File.Delete(file);
        }
        else
            await PS.Execute($"Remove-Item -Recurse -Force \"{dirPath}\"");
    }

    public static async Task<string> CreateDockerFileFromTemplate(ProgrammingLanguage language,
        Dictionary<string, string> variables)
    {
        var path = language switch
        {
            ProgrammingLanguage.Python => AppStorage.PathFromDomain("Templates", "Dockerfile-python"),
            _ => throw new NotImplementedException(language.ToString())
        };

        var text = await File.ReadAllTextAsync(path);

        foreach (var pair in variables)
            text = text.Replace($"%{pair.Key}%", pair.Value);

        return text;
    }
}