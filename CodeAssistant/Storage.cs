using System.IO.Compression;
using CodeAssistant.Services;
using Core;
using Core.Storage;

namespace CodeAssistant;

public static class Storage
{
  private const string TopicDescriptionFile = "topic_description.md";

  public static List<string> GetSnippetTopics() => Directory.GetDirectories(IStorageHandler.Snippets).ToList();

  public static async Task<(Dictionary<string, List<string>> topics, string description)> GetTopicLanguages(
    string topicDirectory)
  {
    var description = "";

    Dictionary<string, List<string>> results = new();

    foreach (var file in Directory.GetFiles(topicDirectory))
    {
      var fileInfo = new FileInfo(file);
      
      // if we find the markdown file containing the topical description -- we shall snag that and continue
      if (fileInfo.Name.Equals(TopicDescriptionFile))
      {
        description = await File.ReadAllTextAsync(file);
        continue;
      }

      var extension = fileInfo.Extension.Replace(".", "");
      var language = string.Empty;// CodeAssistantService.GetLanguage(extension);

      if (results.ContainsKey(language))
        results[language].Add(file);
      else
        results.Add(language, new List<string>());
    }
    
    return (results, description);
  }

  public static async Task AddTopic(string topicName, string topicDescription)
  {
    var directory = Path.Join(IStorageHandler.Snippets, topicName);
    Directory.CreateDirectory(directory);

    await File.WriteAllTextAsync(Path.Join(directory, topicDescription), topicDescription);
  }

  public static void AddToTopic(string topicName, string zipFileLocation, bool deleteZipAfter = true)
  {
    if (!File.Exists(zipFileLocation))
      throw new FileNotFoundException(zipFileLocation);

    if (!zipFileLocation.EndsWith(".zip"))
      throw new NotSupportedException("This method only works with zip files, with the .zip extension");

    var topicPath = Path.Join(IStorageHandler.Snippets, topicName);
    using var archive = ZipFile.OpenRead(zipFileLocation);

    foreach (var entry in archive.Entries)
    {
      var destination = Path.Join(topicPath, entry.Name);
      entry.ExtractToFile(destination);
    }

    // Finally delete the zip file now that it has been consumed (if desired)
    if (deleteZipAfter)
      File.Delete(zipFileLocation);
  }
}