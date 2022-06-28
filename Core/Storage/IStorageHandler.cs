namespace Core.Storage;

public interface IStorageHandler
{
    static string AppDataPath => Path.Join(AppDomain.CurrentDomain.BaseDirectory, "Data");
    static string ToolsProfilePath => Path.Join(AppDataPath, "Profiles");
    static string ScriptsPath => Path.Join(AppDataPath, "Scripts");
    static string ConfigsPath => Path.Join(AppDataPath, "Configs");
    static string TemporaryFileStorage => Path.Join(AppDataPath, "Temp");
    static string Snippets => Path.Join(AppDataPath, "Snippets");
    static string GeneratedReports => Path.Join(AppDomain.CurrentDomain.BaseDirectory, "web", "reports");

    /// <summary>
    /// Get Path from <see cref="AppDomain.CurrentDomain.BaseDirectory"/>
    /// </summary>
    /// <param name="folders"></param>
    /// <returns></returns>
    static string PathFromDomain(params string[] folders)
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
    static string PathFromData(params string[] folders)
    {
        var list = folders.ToList();
        list.Insert(0, AppDataPath);
        return Path.Join(list.ToArray());
    }
    
    static void InitializeStorage()
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