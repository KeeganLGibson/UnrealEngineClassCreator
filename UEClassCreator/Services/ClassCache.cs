using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using UEClassCreator.Models;

namespace UEClassCreator.Services;

public class ClassCache
{
    private static readonly string CacheDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "UEClassCreator", "cache");

    public bool TryLoad(string enginePath, EngineSource source, out List<ClassEntry> entries)
    {
        entries = [];
        string cacheFile = GetCachePath(enginePath);

        if (!File.Exists(cacheFile))
            return false;

        // Source builds are synced via UGS which bumps Build.version on every sync regardless
        // of whether any headers changed — only invalidate launcher installs automatically.
        if (source == EngineSource.LauncherInstall && IsCacheStale(enginePath, cacheFile))
            return false;

        try
        {
            entries = JsonSerializer.Deserialize<List<ClassEntry>>(File.ReadAllText(cacheFile)) ?? [];
            return entries.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    public void Save(string enginePath, List<ClassEntry> entries)
    {
        Directory.CreateDirectory(CacheDir);
        File.WriteAllText(GetCachePath(enginePath), JsonSerializer.Serialize(entries));
    }

    private static string GetCachePath(string enginePath)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(enginePath.ToLowerInvariant()));
        return Path.Combine(CacheDir, Convert.ToHexString(hash).ToLowerInvariant() + ".json");
    }

    private static bool IsCacheStale(string enginePath, string cacheFile)
    {
        string buildVersionPath = Path.Combine(enginePath, "Engine", "Build", "Build.version");
        if (!File.Exists(buildVersionPath))
            return false;

        return File.GetLastWriteTimeUtc(buildVersionPath) > File.GetLastWriteTimeUtc(cacheFile);
    }
}
