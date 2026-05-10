using System.IO;
using System.Text.Json;
using Microsoft.Win32;
using UEClassCreator.Models;

namespace UEClassCreator.Services;

public class EngineLocator
{
    private const string LauncherInstalledPath =
        @"C:\ProgramData\Epic\UnrealEngineLauncher\LauncherInstalled.dat";

    public IReadOnlyList<EngineInstall> FindInstalledEngines()
    {
        var results = new List<EngineInstall>();
        FindLauncherInstalls(results);
        FindSourceBuilds(results);
        return results;
    }

    private static void FindLauncherInstalls(List<EngineInstall> results)
    {
        if (!File.Exists(LauncherInstalledPath))
            return;

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(LauncherInstalledPath));
            if (!doc.RootElement.TryGetProperty("InstallationList", out var list))
                return;

            foreach (var item in list.EnumerateArray())
            {
                if (!item.TryGetProperty("InstallLocation", out var loc) ||
                    !item.TryGetProperty("AppVersion", out var ver))
                    continue;

                string path = loc.GetString() ?? string.Empty;
                string version = ver.GetString() ?? string.Empty;

                if (!Directory.Exists(System.IO.Path.Combine(path, "Engine")))
                    continue;

                results.Add(new EngineInstall(path, EngineSource.LauncherInstall, version));
            }
        }
        catch { /* unreadable launcher data — skip silently */ }
    }

    private static void FindSourceBuilds(List<EngineInstall> results)
    {
        // HKCU\Software\Epic Games\Unreal Engine\Builds holds custom source builds keyed by GUID
        foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
        {
            try
            {
                using var key = RegistryKey
                    .OpenBaseKey(RegistryHive.CurrentUser, view)
                    .OpenSubKey(@"Software\Epic Games\Unreal Engine\Builds");

                if (key is null) continue;

                foreach (string name in key.GetValueNames())
                {
                    string? path = key.GetValue(name) as string;
                    if (string.IsNullOrEmpty(path)) continue;
                    if (!Directory.Exists(System.IO.Path.Combine(path, "Engine"))) continue;
                    if (results.Any(r => r.Path.Equals(path, StringComparison.OrdinalIgnoreCase))) continue;

                    results.Add(new EngineInstall(path, EngineSource.SourceBuild, name));
                }
            }
            catch { /* registry key may not exist */ }
        }
    }
}
