namespace UEClassCreator.Models;

public record EngineInstall(
    string Path,
    EngineSource Source,
    string Version
);
