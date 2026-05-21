using System.Net.Http;
using System.Reflection;
using System.Xml.Linq;

namespace UEClassCreator.Services;

public record UpdateInfo(string Version, string ReleaseUrl);

public class UpdateCheckService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };
    private static readonly XNamespace Atom = "http://www.w3.org/2005/Atom";
    private const string FeedUrl = "https://github.com/KeeganLGibson/UnrealEngineClassCreator/releases.atom";

    public async Task<UpdateInfo?> CheckAsync(CancellationToken ct = default)
    {
        try
        {
            string xml = await Http.GetStringAsync(FeedUrl, ct);
            var doc = XDocument.Parse(xml);

            var entry = doc.Root?.Element(Atom + "entry");
            if (entry is null) return null;

            string? title = entry.Element(Atom + "title")?.Value;
            string? url   = entry.Elements(Atom + "link")
                                 .FirstOrDefault(e => (string?)e.Attribute("rel") == "alternate")
                                 ?.Attribute("href")?.Value;

            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(url)) return null;

            string latestRaw  = title.TrimStart('v');
            string currentRaw = (Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(0, 0, 0)).ToString();

            if (Version.TryParse(latestRaw, out var latest) &&
                Version.TryParse(currentRaw, out var current) &&
                latest > current)
            {
                return new UpdateInfo(latestRaw, url);
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}
