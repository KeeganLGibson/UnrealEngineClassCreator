using System.IO;

namespace UEClassCreator.Models;

public record UProjectEntry(
    string UProjectPath,
    string EnginePath,
    EngineSource EngineSource
)
{
    public string ProjectName      => Path.GetFileNameWithoutExtension(UProjectPath);
    public string ProjectDirectory => Path.GetDirectoryName(UProjectPath) ?? string.Empty;
}
