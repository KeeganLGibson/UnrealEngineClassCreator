using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UEClassCreator.Models;

namespace UEClassCreator.ViewModels;

public partial class ClassDetailViewModel : ObservableObject
{
    private const int SubclassPreviewCount = 5;

    private readonly Action<ClassEntry> _selectClass;
    private readonly IReadOnlyList<ClassEntry> _allSubclasses;

    public ClassEntry Entry { get; }
    public IReadOnlyList<ClassEntry> AncestryChain { get; }
    public int TotalSubclassCount => _allSubclasses.Count;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMoreSubclasses))]
    [NotifyPropertyChangedFor(nameof(HiddenCount))]
    private IReadOnlyList<ClassEntry> _visibleSubclasses = [];

    public bool HasMoreSubclasses => VisibleSubclasses.Count < _allSubclasses.Count;
    public int HiddenCount => _allSubclasses.Count - VisibleSubclasses.Count;

    public ClassDetailViewModel(ClassEntry entry, ClassIndex index, Action<ClassEntry> selectClass)
    {
        Entry          = entry;
        AncestryChain  = index.GetAncestry(entry);
        _allSubclasses = index.GetDirectSubclasses(entry);
        _selectClass   = selectClass;

        _visibleSubclasses = _allSubclasses.Count <= SubclassPreviewCount
            ? _allSubclasses
            : (IReadOnlyList<ClassEntry>)_allSubclasses.Take(SubclassPreviewCount).ToList();
    }

    [RelayCommand]
    private void SelectAncestor(ClassEntry ancestor) => _selectClass(ancestor);

    [RelayCommand]
    private void SelectSubclass(ClassEntry entry) => _selectClass(entry);

    [RelayCommand]
    private void ShowAllSubclasses() => VisibleSubclasses = _allSubclasses;
}
