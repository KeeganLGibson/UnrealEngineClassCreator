using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UEClassCreator.Models;

namespace UEClassCreator.ViewModels;

public partial class ClassDetailViewModel : ObservableObject
{
    private readonly Action<ClassEntry> _selectClass;

    public ClassEntry Entry { get; }
    public IReadOnlyList<ClassEntry> AncestryChain { get; }
    public IReadOnlyList<ClassEntry> DirectSubclasses { get; }

    public ClassDetailViewModel(ClassEntry entry, ClassIndex index, Action<ClassEntry> selectClass)
    {
        Entry = entry;
        AncestryChain = index.GetAncestry(entry);
        DirectSubclasses = index.GetDirectSubclasses(entry);
        _selectClass = selectClass;
    }

    [RelayCommand]
    private void SelectAncestor(ClassEntry ancestor) => _selectClass(ancestor);
}
