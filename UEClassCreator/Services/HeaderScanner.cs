using System.Collections.Concurrent;
using System.IO;
using System.Text.RegularExpressions;
using UEClassCreator.Models;

namespace UEClassCreator.Services;

public class HeaderScanner
{
    // Tested regex from old tool — matches class/struct declarations, not commented lines, not forward declarations
    private static readonly Regex HeaderRegex = new(
        @"^(?!\s*\/\/*\s*)(?:\s*(class|struct)\s*\w*\s+)([UAF]\w+)(?:\s*:\s*public\s+)?(\w*)(?:,\s+\w+\s+\w+)*$(?!;)",
        RegexOptions.Compiled | RegexOptions.Multiline);

    public async Task<List<ClassEntry>> ScanAsync(
        IEnumerable<string> headerFiles,
        EngineSource source,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var results = new ConcurrentBag<ClassEntry>();
        int processed = 0;

        await Parallel.ForEachAsync(headerFiles, cancellationToken, async (file, ct) =>
        {
            string content = await File.ReadAllTextAsync(file, ct);

            foreach (var entry in ParseHeader(content, file, source))
                results.Add(entry);

            progress?.Report(Interlocked.Increment(ref processed));
        });

        return [.. results];
    }

    internal static IEnumerable<ClassEntry> ParseHeader(string content, string filePath, EngineSource source)
    {
        string moduleName = ExtractModuleName(filePath);

        foreach (Match match in HeaderRegex.Matches(content))
        {
            string className = match.Groups[2].Value;
            string parentClass = match.Groups[3].Value;
            yield return new ClassEntry(className, parentClass, moduleName, filePath, source);
        }
    }

    internal static string ExtractModuleName(string filePath)
    {
        string normalized = filePath.Replace('\\', '/');
        string[] parts = normalized.Split('/');

        for (int i = parts.Length - 1; i >= 1; i--)
        {
            if (parts[i].Equals("Public", StringComparison.OrdinalIgnoreCase) ||
                parts[i].Equals("Private", StringComparison.OrdinalIgnoreCase))
            {
                return parts[i - 1];
            }
        }

        return string.Empty;
    }
}
