using System.Text.Json;

namespace EliteRetro.Core.Systems;

/// <summary>
/// Persistent game options stored alongside commander.bin.
/// </summary>
public static class OptionsManager
{
    private const string FileName = "options.json";

    public static string GetSavePath()
    {
        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "EliteRetro");
        Directory.CreateDirectory(appData);
        return Path.Combine(appData, FileName);
    }

    public static bool TryLoad(
        out bool drawWhite,
        out bool drawInvisible,
        out int resolutionIndex,
        out bool fullscreen)
    {
        drawWhite = false;
        drawInvisible = false;
        resolutionIndex = 0;
        fullscreen = false;
        try
        {
            var path = GetSavePath();
            if (!File.Exists(path)) return false;

            var json = File.ReadAllText(path);
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("drawWhite", out var dw))
                drawWhite = dw.GetBoolean();
            if (root.TryGetProperty("drawInvisible", out var di))
                drawInvisible = di.GetBoolean();
            if (root.TryGetProperty("resolutionIndex", out var ri))
                resolutionIndex = ri.GetInt32();
            if (root.TryGetProperty("fullscreen", out var fs))
                fullscreen = fs.GetBoolean();

            return true;
        }
        catch
        {
            return false;
        }
    }

    public static void Save(
        bool drawWhite,
        bool drawInvisible,
        int resolutionIndex,
        bool fullscreen)
    {
        try
        {
            var path = GetSavePath();
            var json = JsonSerializer.Serialize(new
            {
                drawWhite,
                drawInvisible,
                resolutionIndex,
                fullscreen
            }, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
        catch
        {
            // Silently fail — options are non-critical
        }
    }
}
