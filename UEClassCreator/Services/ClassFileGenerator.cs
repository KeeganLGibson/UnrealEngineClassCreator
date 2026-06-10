using System.IO;
using System.Text.RegularExpressions;
using Stubble.Core.Builders;
using UEClassCreator.Models;

namespace UEClassCreator.Services;

public record GenerationRequest(
    string ClassName,
    string Description,
    string OutputPath,
    ClassEntry? ParentClass,
    string ProjectName,
    string CompanyName,
    bool IsStruct = false,
    bool IsUStruct = false,
    string? CustomCopyright = null,
    string ProjectDirectory = ""
);

public class ClassFileGenerator
{
    private static readonly Regex UClassPrefixRegex = new(@"^[AU][A-Z]", RegexOptions.Compiled);
    private static readonly Regex SourceSegmentRegex = new(@"[/\\]source[/\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex PublicPrivateSegmentRegex = new(@"[/\\](public|private)([/\\]|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly string _templatesDir;

    public ClassFileGenerator(string? templatesDir = null)
    {
        _templatesDir = templatesDir ?? Path.Combine(AppContext.BaseDirectory, "Templates");
    }

    // Splits an output path into header (Public) and cpp (Private) directories when the
    // path contains a Public or Private segment after a Source segment. Both return values
    // are the same when no such structure is detected.
    public static (string headerPath, string cppPath) ResolveOutputPaths(string outputPath)
    {
        var sourceMatch = SourceSegmentRegex.Match(outputPath);
        if (!sourceMatch.Success)
            return (outputPath, outputPath);

        string afterSource = outputPath[sourceMatch.Index..];
        var ppMatch = PublicPrivateSegmentRegex.Match(afterSource);
        if (!ppMatch.Success)
            return (outputPath, outputPath);

        // Replace the "Public"/"Private" word in-place, preserving surrounding separators.
        int wordStart = sourceMatch.Index + ppMatch.Index + 1; // +1 skips the leading separator
        int wordEnd = wordStart + ppMatch.Groups[1].Length;

        string headerPath = outputPath[..wordStart] + "Public" + outputPath[wordEnd..];
        string cppPath    = outputPath[..wordStart] + "Private" + outputPath[wordEnd..];
        return (headerPath, cppPath);
    }

    public async Task GenerateAsync(GenerationRequest request)
    {
        var (headerPath, cppPath) = ResolveOutputPaths(request.OutputPath);
        var stubble = new StubbleBuilder().Build();
        var data = BuildData(request, headerPath);

        Directory.CreateDirectory(headerPath);

        string fileName = GetFileName(request.ClassName);
        bool isStruct = request.IsStruct
            || (request.ParentClass is { } pc
                && pc.ClassName.Length > 1
                && pc.ClassName[0] == 'F'
                && char.IsUpper(pc.ClassName[1]));

        if (isStruct)
        {
            string template = await File.ReadAllTextAsync(ResolveTemplate("Struct.mustache", request.ProjectDirectory));
            await File.WriteAllTextAsync(
                Path.Combine(headerPath, fileName + ".h"),
                stubble.Render(template, data));
        }
        else
        {
            string headerTemplate = await File.ReadAllTextAsync(ResolveTemplate("Header.mustache", request.ProjectDirectory));
            await File.WriteAllTextAsync(
                Path.Combine(headerPath, fileName + ".h"),
                stubble.Render(headerTemplate, data));

            Directory.CreateDirectory(cppPath);
            string cppTemplate = await File.ReadAllTextAsync(ResolveTemplate("Cpp.mustache", request.ProjectDirectory));
            await File.WriteAllTextAsync(
                Path.Combine(cppPath, fileName + ".cpp"),
                stubble.Render(cppTemplate, data));
        }
    }

    // Checks {projectDir}/build/ClassCreator/{name} first; falls back to the app's Templates dir.
    private string ResolveTemplate(string fileName, string projectDirectory)
    {
        if (!string.IsNullOrEmpty(projectDirectory))
        {
            string projectOverride = Path.Combine(projectDirectory, "build", "ClassCreator", fileName);
            if (File.Exists(projectOverride))
                return projectOverride;
        }
        return Path.Combine(_templatesDir, fileName);
    }

    internal Dictionary<string, object> BuildData(GenerationRequest request, string? headerPath = null)
    {
        string fileName = GetFileName(request.ClassName);
        bool hasParent = request.ParentClass is not null;
        bool isUClass = hasParent && UClassPrefixRegex.IsMatch(request.ParentClass!.ClassName);
        bool isGameModule = request.ParentClass?.Source == EngineSource.GameProject;

        var data = new Dictionary<string, object>
        {
            ["Class"] = request.ClassName,
            ["FileName"] = fileName,
            ["ParentClass"] = request.ParentClass?.ClassName ?? string.Empty,
            ["ParentClassSource"] = hasParent ? ComputeParentClassSource(request, headerPath ?? request.OutputPath) : string.Empty,
            ["ModuleName"] = request.ParentClass?.ModuleName ?? string.Empty,
            ["ProjectName"] = request.ProjectName,
            ["ProjectCompany"] = request.CompanyName,
            ["Year"] = DateTime.Now.Year.ToString(),
            ["Description"] = string.IsNullOrWhiteSpace(request.Description) ? "TODO:" : request.Description,
            ["bIsUClass"] = isUClass,
            ["bIsGameModule"] = isGameModule,
            ["bHasParent"] = hasParent,
            ["bIsUStruct"] = isUClass || request.IsUStruct,
        };

        if (!string.IsNullOrWhiteSpace(request.CustomCopyright))
            data["CustomCopyright"] = request.CustomCopyright;

        return data;
    }

    private static string ComputeParentClassSource(GenerationRequest request, string fromPath)
    {
        string headerPath = request.ParentClass!.HeaderPath;
        string normalized = headerPath.Replace('\\', '/');
        string[] parts = normalized.Split('/');
        string fileName = parts[^1];
        string headerDir = string.Join("/", parts[..^1]);
        string fromNormalized = fromPath.Replace('\\', '/').TrimEnd('/');

        // Same directory as output → no path prefix needed.
        if (headerDir.Equals(fromNormalized, StringComparison.OrdinalIgnoreCase))
            return fileName;

        // Has a Public/Private segment → path relative to that directory.
        for (int i = parts.Length - 1; i >= 1; i--)
        {
            if (parts[i].Equals("Public", StringComparison.OrdinalIgnoreCase) ||
                parts[i].Equals("Private", StringComparison.OrdinalIgnoreCase))
            {
                return string.Join("/", parts[(i + 1)..]);
            }
        }

        // No Public/Private → path relative to Source/{Module}/ (the module root).
        // UBT adds the module root to the include search path for flat-layout modules.
        for (int i = 0; i < parts.Length - 2; i++)
        {
            if (parts[i].Equals("Source", StringComparison.OrdinalIgnoreCase))
            {
                int moduleContentStart = i + 2; // skip Source/ and ModuleName/
                if (moduleContentStart < parts.Length)
                    return string.Join("/", parts[moduleContentStart..]);
                break;
            }
        }

        // Fallback: filesystem-relative path from the output directory.
        string parentDir = Path.GetDirectoryName(headerPath) ?? string.Empty;
        string relative = Path.GetRelativePath(fromPath, parentDir);
        return relative.Replace('\\', '/').TrimEnd('/') + "/" + fileName;
    }

    internal static string GetFileName(string className)
    {
        if (className.Length > 1
            && (className[0] == 'U' || className[0] == 'A' || className[0] == 'F')
            && char.IsUpper(className[1]))
        {
            return className[1..];
        }
        return className;
    }
}
