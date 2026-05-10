using System.IO;
using System.Text.Json;
using Microsoft.Win32;
using UEClassCreator.Models;

namespace UEClassCreator.Services;

public class EngineLocator
{
    private const string LauncherInstalledPath =
        @"C:\ProgramData\Epic\UnrealEngineLauncher\LauncherInstalled.dat";

    /// <summary>
    /// Resolves the engine for a .uproject file using the same lookup chain as the Epic launcher:
    ///   1. Co-located source build — ../Engine/ exists next to the project folder (UGS layout)
    ///   2. HKLM registry — launcher binary installs keyed by version string
    ///   3. HKCU registry — source builds keyed by GUID
    ///   4. LauncherInstalled.dat — fallback for launcher installs
    /// </summary>
    public UProjectEntry? ResolveForProject(string uprojectPath)
    {
        if (!File.Exists(uprojectPath)) return null;

        string projectDir = Path.GetDirectoryName(uprojectPath)!;
        string association = ReadEngineAssociation(uprojectPath);

        // 1. Co-located source build (e.g. D:\Work\PVE\PvE\.uproject → D:\Work\PVE\Engine)
        string colocatedEngine = Path.GetFullPath(Path.Combine(projectDir, "..", "Engine"));
        if (Directory.Exists(colocatedEngine))
        {
            string engineRoot = Path.GetDirectoryName(colocatedEngine)!;
            return new UProjectEntry(uprojectPath, engineRoot, EngineSource.SourceBuild);
        }

        // 2. HKLM — launcher binary installs
        string? path = TryRegistryLauncher(association);
        if (path != null) return new UProjectEntry(uprojectPath, path, EngineSource.LauncherInstall);

        // 3. HKCU — source builds registered by GUID
        path = TryRegistrySourceBuild(association);
        if (path != null) return new UProjectEntry(uprojectPath, path, EngineSource.SourceBuild);

        // 4. LauncherInstalled.dat
        path = TryLauncherDat(association);
        if (path != null) return new UProjectEntry(uprojectPath, path, EngineSource.LauncherInstall);

        return null;
    }

    private static string ReadEngineAssociation(string uprojectPath)
    {
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(uprojectPath));
            if (doc.RootElement.TryGetProperty("EngineAssociation", out var val))
                return val.GetString() ?? string.Empty;
        }
        catch { }
        return string.Empty;
    }

    private static string? TryRegistryLauncher(string association)
    {
        if (string.IsNullOrEmpty(association)) return null;

        foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
        {
            try
            {
                using var key = RegistryKey
                    .OpenBaseKey(RegistryHive.LocalMachine, view)
                    .OpenSubKey($@"SOFTWARE\EpicGames\Unreal Engine\{association}");

                string? path = key?.GetValue("InstalledDirectory") as string;
                if (!string.IsNullOrEmpty(path) && Directory.Exists(Path.Combine(path, "Engine")))
                    return path;
            }
            catch { }
        }
        return null;
    }

    private static string? TryRegistrySourceBuild(string association)
    {
        if (string.IsNullOrEmpty(association)) return null;

        foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
        {
            try
            {
                using var key = RegistryKey
                    .OpenBaseKey(RegistryHive.CurrentUser, view)
                    .OpenSubKey(@"Software\Epic Games\Unreal Engine\Builds");

                string? path = key?.GetValue(association) as string;
                if (!string.IsNullOrEmpty(path) && Directory.Exists(Path.Combine(path, "Engine")))
                    return path;
            }
            catch { }
        }
        return null;
    }

    private static string? TryLauncherDat(string association)
    {
        if (!File.Exists(LauncherInstalledPath)) return null;
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(LauncherInstalledPath));
            if (!doc.RootElement.TryGetProperty("InstallationList", out var list)) return null;

            foreach (var item in list.EnumerateArray())
            {
                string version = item.TryGetProperty("AppVersion", out var v) ? v.GetString() ?? "" : "";
                string path    = item.TryGetProperty("InstallLocation", out var l) ? l.GetString() ?? "" : "";

                if (version.StartsWith(association, StringComparison.OrdinalIgnoreCase)
                    && Directory.Exists(Path.Combine(path, "Engine")))
                    return path;
            }
        }
        catch { }
        return null;
    }
}
