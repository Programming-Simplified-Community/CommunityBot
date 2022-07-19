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

    /// <summary>
    /// If <paramref name="dirPath"/> does not exist, create it
    /// </summary>
    /// <param name="dirPath"></param>
    public static void EnsureDir(string dirPath)
    {
        if (!Directory.Exists(dirPath))
            Directory.CreateDirectory(dirPath);
    }
    
    /// <summary>
    /// Delete a directory's contents recursively.
    /// <p>If <paramref name="filesOnly"/>, it will keep the folders... just empty</p>
    /// </summary>
    /// <param name="dirPath"></param>
    /// <param name="filesOnly"></param>
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
    
    /// <summary>
    /// Takes a <paramref name="language"/> and replaces each variable using "%VARNAME%" format with values from
    /// <paramref name="variables"/>. AKA - each key in the dictionary must represent a variable name inside the template.
    /// </summary>
    /// <param name="language"></param>
    /// <param name="variables"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException">When <paramref name="language"/> is not implemented</exception>
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