using UEClassCreator.Models;
using UEClassCreator.Services;

namespace UEClassCreator.Tests;

public class HeaderScannerTests
{
    private const string FakePath = @"C:/Engine/Source/Runtime/Engine/Public/GameFramework/Actor.h";

    // --- ParseHeader ---

    [Fact]
    public void ParseHeader_CrlfLineEndings_ReturnsEntries()
    {
        // Unreal Engine headers on Windows use CRLF. The old tool used ReadLine() which
        // stripped \r; we read the whole file so we must normalise before matching.
        string content = "class ENGINE_API AActor : public UObject\r\n{\r\n};\r\nclass ENGINE_API APawn : public AActor\r\n{\r\n};";
        var results = HeaderScanner.ParseHeader(content, FakePath, EngineSource.LauncherInstall).ToList();

        Assert.Equal(2, results.Count);
        Assert.Contains(results, e => e.ClassName == "AActor");
        Assert.Contains(results, e => e.ClassName == "APawn");
    }

    [Fact]
    public void ParseHeader_SimpleClass_ReturnsEntry()
    {
        string content = "class ENGINE_API AActor : public UObject\n{\n};";
        var results = HeaderScanner.ParseHeader(content, FakePath, EngineSource.LauncherInstall).ToList();

        Assert.Single(results);
        Assert.Equal("AActor", results[0].ClassName);
        Assert.Equal("UObject", results[0].ParentClass);
    }

    [Fact]
    public void ParseHeader_Struct_ReturnsEntry()
    {
        string content = "struct ENGINE_API FMyStruct : public FTableRowBase\n{\n};";
        var results = HeaderScanner.ParseHeader(content, FakePath, EngineSource.LauncherInstall).ToList();

        Assert.Single(results);
        Assert.Equal("FMyStruct", results[0].ClassName);
        Assert.Equal("FTableRowBase", results[0].ParentClass);
    }

    [Fact]
    public void ParseHeader_NoParent_ReturnsEmptyParentClass()
    {
        string content = "class ENGINE_API UObject\n{\n};";
        var results = HeaderScanner.ParseHeader(content, FakePath, EngineSource.LauncherInstall).ToList();

        Assert.Single(results);
        Assert.Equal("UObject", results[0].ClassName);
        Assert.Equal(string.Empty, results[0].ParentClass);
    }

    [Fact]
    public void ParseHeader_CommentedOutClass_IsSkipped()
    {
        string content = "// class ENGINE_API AActor : public UObject";
        var results = HeaderScanner.ParseHeader(content, FakePath, EngineSource.LauncherInstall).ToList();
        Assert.Empty(results);
    }

    [Fact]
    public void ParseHeader_ForwardDeclaration_IsSkipped()
    {
        string content = "class AActor;";
        var results = HeaderScanner.ParseHeader(content, FakePath, EngineSource.LauncherInstall).ToList();
        Assert.Empty(results);
    }

    [Fact]
    public void ParseHeader_MultipleClasses_ReturnsAll()
    {
        string content = """
            class ENGINE_API AActor : public UObject
            {
            };
            class ENGINE_API APawn : public AActor
            {
            };
            """;

        var results = HeaderScanner.ParseHeader(content, FakePath, EngineSource.LauncherInstall).ToList();
        Assert.Equal(2, results.Count);
    }

    [Fact]
    public void ParseHeader_SetsSourceAndHeaderPath()
    {
        string content = "class ENGINE_API AActor : public UObject\n{\n};";
        var result = HeaderScanner.ParseHeader(content, FakePath, EngineSource.SourceBuild).First();

        Assert.Equal(EngineSource.SourceBuild, result.Source);
        Assert.Equal(FakePath, result.HeaderPath);
    }

    // --- ExtractModuleName ---

    [Theory]
    [InlineData(@"C:/Engine/Source/Runtime/Engine/Public/GameFramework/Actor.h", "Engine")]
    [InlineData(@"C:\Engine\Source\Runtime\Core\Public\Misc\App.h", "Core")]
    [InlineData(@"C:/Engine/Source/Runtime/InputCore/Private/InputCoreTypes.cpp", "InputCore")]
    public void ExtractModuleName_KnownPaths_ReturnsCorrectModule(string path, string expected)
    {
        Assert.Equal(expected, HeaderScanner.ExtractModuleName(path));
    }

    [Fact]
    public void ExtractModuleName_NoPublicOrPrivate_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, HeaderScanner.ExtractModuleName(@"C:/SomeRandom/Path/File.h"));
    }
}
