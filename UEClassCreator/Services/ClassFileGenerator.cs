using System.IO;
using System.Text.RegularExpressions;
using Stubble.Core.Builders;
using UEClassCreator.Models;

namespace UEClassCreator.Services;

public record GenerationRequest(
    string ClassName,
    string Description,
    string OutputPath,
    ClassEntry ParentClass,
    string ProjectName,
    string CompanyName,
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
        bool isStruct = request.ParentClass.ClassName.Length > 1
            && request.ParentClass.ClassName[0] == 'F'
            && char.IsUpper(request.ParentClass.ClassName[1]);

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
        bool isUClass = UClassPrefixRegex.IsMatch(request.ParentClass.ClassName);
        bool isGameModule = request.ParentClass.Source == EngineSource.GameProject;

        var data = new Dictionary<string, object>
        {
            ["Class"] = request.ClassName,
            ["FileName"] = fileName,
            ["ParentClass"] = request.ParentClass.ClassName,
            ["ParentClassSource"] = ComputeParentClassSource(request, headerPath ?? request.OutputPath),
            ["ModuleName"] = request.ParentClass.ModuleName,
            ["ProjectName"] = request.ProjectName,
            ["ProjectCompany"] = request.CompanyName,
            ["Year"] = DateTime.Now.Year.ToString(),
            ["Description"] = string.IsNullOrWhiteSpace(request.Description) ? "TODO:" : request.Description,
            ["bIsUClass"] = isUClass,
            ["bIsGameModule"] = isGameModule,
        };

        if (!string.IsNullOrWhiteSpace(request.CustomCopyright))
            data["CustomCopyright"] = request.CustomCopyright;

        return data;
    }

    private static string ComputeParentClassSource(GenerationRequest request, string fromPath)
    {
        string parentDir = Path.GetDirectoryName(request.ParentClass.HeaderPath) ?? string.Empty;
        string relative = Path.GetRelativePath(fromPath, parentDir);
        return relative.Replace('\\', '/').TrimEnd('/') + "/" + Path.GetFileName(request.ParentClass.HeaderPath);
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
