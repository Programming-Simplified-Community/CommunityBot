namespace Core.Storage;

public class AppStorage : IStorageHandler
{
    public static string AppDataPath => Path.Join(AppDomain.CurrentDomain.BaseDirectory, "Data");
    public static string ToolsProfilePath => Path.Join(AppDataPath, "Profiles");
    public static string ScriptsPath => PathFromData("Scripts");
    public static string ReportsPath => Path.Join(AppDataPath, "Reports");
    public static string ConfigsPath => Path.Join(AppDataPath, "Configs");
    public static string TemporaryFileStorage => Path.Join(AppDataPath, "Temp");
    public static string Snippets => Path.Join(AppDataPath, "Snippets");
    public static string GeneratedReports => Path.Join(AppDomain.CurrentDomain.BaseDirectory, "web", "reports");

    /// <summary>
    /// Get Path from <see cref="AppDomain.CurrentDomain.BaseDirectory"/>
    /// </summary>
    /// <param name="folders"></param>
    /// <returns></returns>
    public static string PathFromDomain(params string[] folders)
    {
        var list = folders.ToList();
        list.Insert(0, AppDomain.CurrentDomain.BaseDirectory);
        return Path.Join(list.ToArray());
    }
    
    /// <summary>
    /// Get path from <see cref="AppDomain.CurrentDomain.BaseDirectory"/>/Data
    /// </summary>
    /// <param name="folders"></param>
    /// <returns></returns>
    public static string PathFromData(params string[] folders)
    {
        var list = folders.ToList();
        list.Insert(0, AppDataPath);
        return Path.Join(list.ToArray());
    }
    
    public static void InitializeStorage()
    {
        string[] paths =
        {
            AppDataPath, ToolsProfilePath, ScriptsPath, ConfigsPath, TemporaryFileStorage, Snippets, GeneratedReports
        };
        
        foreach(var path in paths)
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
    }
}