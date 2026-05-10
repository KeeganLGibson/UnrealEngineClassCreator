namespace UEClassCreator.Models;

public class ClassIndex
{
    private readonly List<ClassEntry> _entries;
    private readonly Dictionary<string, ClassEntry> _byName;
    private readonly ILookup<string, ClassEntry> _byParent;

    public ClassIndex(IEnumerable<ClassEntry> entries)
    {
        _entries  = [.. entries];
        _byName   = _entries.GroupBy(e => e.ClassName).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
        _byParent = _entries.ToLookup(e => e.ParentClass, StringComparer.Ordinal);
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
    // e.g. for ACharacter: [UObject, AActor, APawn]
    public IReadOnlyList<ClassEntry> GetAncestry(ClassEntry entry)
    {
        var chain   = new List<ClassEntry>();
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var current = entry;

        while (!string.IsNullOrEmpty(current.ParentClass))
        {
            if (!visited.Add(current.ParentClass)) break;
            if (!_byName.TryGetValue(current.ParentClass, out var parent)) break;
            chain.Add(parent);
            current = parent;
        }

        chain.Reverse();
        return chain;
    }

    public IReadOnlyList<ClassEntry> GetDirectSubclasses(ClassEntry entry) =>
        _byParent[entry.ClassName].ToList();

    // BFS from UObject using the pre-built parent→children lookup.
    public IReadOnlySet<string> GetUObjectDescendantNames()
    {
        var result = new HashSet<string> { "UObject" };
        var queue  = new Queue<string>();
        queue.Enqueue("UObject");

        while (queue.Count > 0)
        {
            string parent = queue.Dequeue();
            foreach (var child in _byParent[parent])
            {
                if (result.Add(child.ClassName))
                    queue.Enqueue(child.ClassName);
            }
        }

        return result;
    }
}
