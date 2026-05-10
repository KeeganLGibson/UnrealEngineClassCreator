namespace UEClassCreator.Models;

public class ClassIndex
{
    private readonly List<ClassEntry> _entries;

    public ClassIndex(IEnumerable<ClassEntry> entries)
    {
        _entries = [.. entries];
    }

    public IReadOnlyList<ClassEntry> All => _entries;

    public IReadOnlyList<ClassEntry> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return _entries;

        return _entries
            .Where(e =>
                e.ClassName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                e.ParentClass.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    // Returns ancestry chain from root down to (but not including) the given entry.
    // e.g. for ACharacter: [UObject, UActorComponent, ..., APawn]
    public IReadOnlyList<ClassEntry> GetAncestry(ClassEntry entry)
    {
        var chain = new List<ClassEntry>();
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var current = entry;

        while (!string.IsNullOrEmpty(current.ParentClass))
        {
            if (!visited.Add(current.ParentClass))
                break; // cycle guard

            var parent = _entries.FirstOrDefault(e => e.ClassName == current.ParentClass);
            if (parent is null)
                break;

            chain.Add(parent);
            current = parent;
        }

        chain.Reverse();
        return chain;
    }

    public IReadOnlyList<ClassEntry> GetDirectSubclasses(ClassEntry entry)
    {
        return _entries
            .Where(e => e.ParentClass == entry.ClassName)
            .ToList();
    }
}
