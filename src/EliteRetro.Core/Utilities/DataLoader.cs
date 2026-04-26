namespace EliteRetro.Core.Utilities;

/// <summary>
/// Loads ship data files from the docs directory.
/// </summary>
public static class DataLoader
{
    private static string _basePath = "docs";

    /// <summary>
    /// Set the base path for data files. Call before loading if using a custom path.
    /// </summary>
    public static void SetBasePath(string path) => _basePath = path;

    /// <summary>
    /// Load a text file from the docs directory.
    /// </summary>
    public static string LoadFile(string fileName)
    {
        var path = Path.Combine(_basePath, fileName);
        return File.ReadAllText(path);
    }

    /// <summary>
    /// Check if a data file exists.
    /// </summary>
    public static bool FileExists(string fileName)
    {
        var path = Path.Combine(_basePath, fileName);
        return File.Exists(path);
    }
}
