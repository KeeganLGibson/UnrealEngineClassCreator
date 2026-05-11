using UEClassCreator.Models;
using UEClassCreator.Services;

namespace UEClassCreator.Tests;

public class ClassFileGeneratorTests
{
    private static readonly ClassEntry EngineParent = new(
        "AActor",
        "UObject",
        "Engine",
        @"C:/Engine/Source/Runtime/Engine/Public/GameFramework/Actor.h",
        EngineSource.LauncherInstall
    );

    private static readonly ClassEntry GameParent = new(
        "AMyBase",
        "AActor",
        "MyGame",
        @"C:/Projects/MyGame/Source/MyGame/Public/MyBase.h",
        EngineSource.GameProject
    );

    private static GenerationRequest MakeRequest(
        ClassEntry? parent = null,
        string className = "AMyActor",
        string outputPath = @"C:/Projects/MyGame/Source/MyGame",
        string description = "My actor class") =>
        new(className, description, outputPath, parent ?? EngineParent, "MyGame", "MyCompany");

    // --- GetFileName ---

    [Theory]
    [InlineData("AMyActor", "MyActor")]
    [InlineData("UMyComponent", "MyComponent")]
    [InlineData("FMyStruct", "MyStruct")]
    [InlineData("MyClass", "MyClass")]   // no prefix — unchanged
    [InlineData("A", "A")]              // too short — unchanged
    public void GetFileName_StripsPrefix(string className, string expected)
    {
        Assert.Equal(expected, ClassFileGenerator.GetFileName(className));
    }

    // --- BuildData ---

    [Fact]
    public void BuildData_ContainsRequiredKeys()
    {
        var gen = new ClassFileGenerator();
        var data = gen.BuildData(MakeRequest());

        Assert.True(data.ContainsKey("Class"));
        Assert.True(data.ContainsKey("FileName"));
        Assert.True(data.ContainsKey("ParentClass"));
        Assert.True(data.ContainsKey("ParentClassSource"));
        Assert.True(data.ContainsKey("bIsUClass"));
        Assert.True(data.ContainsKey("bIsGameModule"));
        Assert.True(data.ContainsKey("Year"));
        Assert.True(data.ContainsKey("Description"));
    }

    [Fact]
    public void BuildData_UClassParent_SetsIsUClassTrue()
    {
        var data = new ClassFileGenerator().BuildData(MakeRequest());
        Assert.Equal(true, data["bIsUClass"]);
    }

    [Fact]
    public void BuildData_GameProjectParent_SetsIsGameModuleTrue()
    {
        var data = new ClassFileGenerator().BuildData(MakeRequest(parent: GameParent));
        Assert.Equal(true, data["bIsGameModule"]);
    }

    [Fact]
    public void BuildData_EngineParent_SetsIsGameModuleFalse()
    {
        var data = new ClassFileGenerator().BuildData(MakeRequest());
        Assert.Equal(false, data["bIsGameModule"]);
    }

    [Fact]
    public void BuildData_EmptyDescription_DefaultsToTodo()
    {
        var req = MakeRequest() with { Description = "" };
        var data = new ClassFileGenerator().BuildData(req);
        Assert.Equal("TODO:", data["Description"]);
    }

    [Fact]
    public void BuildData_CustomCopyright_IncludedWhenSet()
    {
        var req = new GenerationRequest("AMyActor", "desc", @"C:/out", EngineParent, "MyGame", "Co", "Copyright 2026 Me");
        var data = new ClassFileGenerator().BuildData(req);
        Assert.True(data.ContainsKey("CustomCopyright"));
        Assert.Equal("Copyright 2026 Me", data["CustomCopyright"]);
    }

    [Fact]
    public void BuildData_NoCopyright_KeyAbsent()
    {
        var data = new ClassFileGenerator().BuildData(MakeRequest());
        Assert.False(data.ContainsKey("CustomCopyright"));
    }

    [Fact]
    public void BuildData_ParentClassSource_UsesForwardSlashes()
    {
        var data = new ClassFileGenerator().BuildData(MakeRequest());
        string source = (string)data["ParentClassSource"];
        Assert.DoesNotContain('\\', source);
    }

    // --- Project template overrides ---

    [Fact]
    public async Task GenerateAsync_UsesProjectTemplate_WhenOverrideExists()
    {
        // Arrange: create a temp project dir with a custom Header.mustache
        string projectDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        string overrideDir = Path.Combine(projectDir, "build", "ClassCreator");
        Directory.CreateDirectory(overrideDir);
        await File.WriteAllTextAsync(Path.Combine(overrideDir, "Header.mustache"), "PROJECT_OVERRIDE {{Class}}");
        await File.WriteAllTextAsync(Path.Combine(overrideDir, "Cpp.mustache"), "CPP {{FileName}}");

        string outputDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        try
        {
            var gen = new ClassFileGenerator();
            var request = new GenerationRequest(
                "AMyActor", "", outputDir, EngineParent, "TestGame", "TestCo",
                ProjectDirectory: projectDir);

            await gen.GenerateAsync(request);

            string headerContent = await File.ReadAllTextAsync(Path.Combine(outputDir, "MyActor.h"));
            Assert.Contains("PROJECT_OVERRIDE", headerContent);
        }
        finally
        {
            if (Directory.Exists(projectDir)) Directory.Delete(projectDir, recursive: true);
            if (Directory.Exists(outputDir))  Directory.Delete(outputDir,  recursive: true);
        }
    }

    [Fact]
    public async Task GenerateAsync_FallsBackToDefaultTemplate_WhenNoOverride()
    {
        string outputDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        string templatesDir = Path.Combine(AppContext.BaseDirectory, "Templates");

        // Only run if the app templates are present (i.e. in a full build output)
        if (!Directory.Exists(templatesDir))
            return;

        try
        {
            var gen = new ClassFileGenerator();
            var request = new GenerationRequest(
                "AMyActor", "", outputDir, EngineParent, "TestGame", "TestCo",
                ProjectDirectory: Path.GetTempPath()); // no build/ClassCreator here

            await gen.GenerateAsync(request);

            Assert.True(File.Exists(Path.Combine(outputDir, "MyActor.h")));
        }
        finally
        {
            if (Directory.Exists(outputDir)) Directory.Delete(outputDir, recursive: true);
        }
    }
}
