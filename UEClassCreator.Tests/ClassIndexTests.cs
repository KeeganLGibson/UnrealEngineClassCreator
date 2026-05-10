using UEClassCreator.Models;

namespace UEClassCreator.Tests;

public class ClassIndexTests
{
    private static ClassEntry E(string name, string parent = "") =>
        new(name, parent, "Engine", "/fake/path.h", EngineSource.LauncherInstall);

    private static ClassIndex BuildIndex() => new([
        E("UObject"),
        E("UActorComponent", "UObject"),
        E("USceneComponent", "UActorComponent"),
        E("APawn", "AActor"),
        E("ACharacter", "APawn"),
        E("AActor", "UObject"),
        E("AMyActor", "AActor"),
    ]);

    // --- Search ---

    [Fact]
    public void Search_EmptyQuery_ReturnsAll()
    {
        var index = BuildIndex();
        Assert.Equal(index.All.Count, index.Search("").Count);
    }

    [Fact]
    public void Search_WhitespaceQuery_ReturnsAll()
    {
        var index = BuildIndex();
        Assert.Equal(index.All.Count, index.Search("   ").Count);
    }

    [Fact]
    public void Search_MatchesClassName_CaseInsensitive()
    {
        var index = BuildIndex();
        var results = index.Search("character");
        Assert.Single(results);
        Assert.Equal("ACharacter", results[0].ClassName);
    }

    [Fact]
    public void Search_MatchesParentClass()
    {
        var index = BuildIndex();
        // "APawn" is the parent of ACharacter — searching "pawn" should find both APawn and ACharacter
        var results = index.Search("pawn");
        Assert.Contains(results, e => e.ClassName == "APawn");
        Assert.Contains(results, e => e.ClassName == "ACharacter");
    }

    [Fact]
    public void Search_NoMatch_ReturnsEmpty()
    {
        var index = BuildIndex();
        Assert.Empty(index.Search("XyzNoMatch"));
    }

    // --- Ancestry ---

    [Fact]
    public void GetAncestry_RootClass_ReturnsEmpty()
    {
        var index = BuildIndex();
        var uobject = index.All.First(e => e.ClassName == "UObject");
        Assert.Empty(index.GetAncestry(uobject));
    }

    [Fact]
    public void GetAncestry_ReturnsChainRootFirst()
    {
        var index = BuildIndex();
        var acharacter = index.All.First(e => e.ClassName == "ACharacter");
        var ancestry = index.GetAncestry(acharacter);

        // Expected: UObject → AActor → APawn
        Assert.Equal(3, ancestry.Count);
        Assert.Equal("UObject", ancestry[0].ClassName);
        Assert.Equal("AActor", ancestry[1].ClassName);
        Assert.Equal("APawn", ancestry[2].ClassName);
    }

    [Fact]
    public void GetAncestry_StopsAtUnknownParent()
    {
        // APawn's parent is AActor, AActor's parent is UObject, UObject has no parent
        var index = new ClassIndex([E("AMyClass", "AUnknownBase")]);
        var entry = index.All[0];
        Assert.Empty(index.GetAncestry(entry));
    }

    [Fact]
    public void GetAncestry_CycleGuard_DoesNotHang()
    {
        var index = new ClassIndex([
            E("A", "B"),
            E("B", "A"),
        ]);
        var a = index.All.First(e => e.ClassName == "A");
        var result = index.GetAncestry(a);
        Assert.True(result.Count <= 2);
    }

    // --- Direct subclasses ---

    [Fact]
    public void GetDirectSubclasses_ReturnsOnlyImmediateChildren()
    {
        var index = BuildIndex();
        var uobject = index.All.First(e => e.ClassName == "UObject");
        var subs = index.GetDirectSubclasses(uobject);

        // UActorComponent and AActor are direct children of UObject
        Assert.Equal(2, subs.Count);
        Assert.Contains(subs, e => e.ClassName == "UActorComponent");
        Assert.Contains(subs, e => e.ClassName == "AActor");
    }

    [Fact]
    public void GetDirectSubclasses_LeafClass_ReturnsEmpty()
    {
        var index = BuildIndex();
        var acharacter = index.All.First(e => e.ClassName == "ACharacter");
        Assert.Empty(index.GetDirectSubclasses(acharacter));
    }
}
