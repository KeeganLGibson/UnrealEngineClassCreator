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
    string? CustomCopyright = null
);

public class ClassFileGenerator
{
    private static readonly Regex UClassPrefixRegex = new(@"^[AU][A-Z]", RegexOptions.Compiled);

    private readonly string _templatesDir;

    public ClassFileGenerator(string? templatesDir = null)
    {
        _templatesDir = templatesDir ?? Path.Combine(AppContext.BaseDirectory, "Templates");
    }

    public async Task GenerateAsync(GenerationRequest request)
    {
        var stubble = new StubbleBuilder().Build();
        var data = BuildData(request);

        Directory.CreateDirectory(request.OutputPath);

        string fileName = GetFileName(request.ClassName);
        bool isStruct = request.ParentClass.ClassName.Length > 1
            && request.ParentClass.ClassName[0] == 'F'
            && char.IsUpper(request.ParentClass.ClassName[1]);

        if (isStruct)
        {
            string template = await File.ReadAllTextAsync(Path.Combine(_templatesDir, "Struct.mustache"));
            await File.WriteAllTextAsync(
                Path.Combine(request.OutputPath, fileName + ".h"),
                stubble.Render(template, data));
        }
        else
        {
            string headerTemplate = await File.ReadAllTextAsync(Path.Combine(_templatesDir, "Header.mustache"));
            await File.WriteAllTextAsync(
                Path.Combine(request.OutputPath, fileName + ".h"),
                stubble.Render(headerTemplate, data));

            string cppTemplate = await File.ReadAllTextAsync(Path.Combine(_templatesDir, "Cpp.mustache"));
            await File.WriteAllTextAsync(
                Path.Combine(request.OutputPath, fileName + ".cpp"),
                stubble.Render(cppTemplate, data));
        }
    }

    internal Dictionary<string, object> BuildData(GenerationRequest request)
    {
        string fileName = GetFileName(request.ClassName);
        bool isUClass = UClassPrefixRegex.IsMatch(request.ParentClass.ClassName);
        bool isGameModule = request.ParentClass.Source == EngineSource.GameProject;

        var data = new Dictionary<string, object>
        {
            ["Class"] = request.ClassName,
            ["FileName"] = fileName,
            ["ParentClass"] = request.ParentClass.ClassName,
            ["ParentClassSource"] = ComputeParentClassSource(request),
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

    private static string ComputeParentClassSource(GenerationRequest request)
    {
        string parentDir = Path.GetDirectoryName(request.ParentClass.HeaderPath) ?? string.Empty;
        string relative = Path.GetRelativePath(request.OutputPath, parentDir);
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
