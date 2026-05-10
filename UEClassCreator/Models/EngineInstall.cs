namespace UEClassCreator.Models;

public record EngineInstall(
    string Path,
    EngineSource Source,
    string Version
)
{
    // "5.4.4-37649993+++UE5+Release-5.4" → "UE 5.4.4 (Launcher)"
    // Source build GUID → "Source Build"
    public string ShortVersion => Source == EngineSource.SourceBuild
        ? "Source Build"
        : "UE " + (Version.IndexOf('-') is > 0 and int i ? Version[..i] : Version) + " (Launcher)";
};
