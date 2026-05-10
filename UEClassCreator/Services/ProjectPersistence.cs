using System.IO;
using System.Text.Json;

namespace UEClassCreator.Services;

public class ProjectPersistence
{
    private static readonly string SettingsPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "UEClassCreator", "projects.json");

    public List<string> Load()
    {
        if (!File.Exists(SettingsPath)) return [];
        try { return JsonSerializer.Deserialize<List<string>>(File.ReadAllText(SettingsPath)) ?? []; }
        catch { return []; }
    }

    public void Save(IEnumerable<string> projectPaths)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(projectPaths.ToList()));
        }
        catch { /* best-effort */ }
    }
}
