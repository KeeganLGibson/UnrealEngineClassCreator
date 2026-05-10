namespace UEClassCreator.Models;

public record ClassEntry(
    string ClassName,
    string ParentClass,
    string ModuleName,
    string HeaderPath,
    EngineSource Source
);
