using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UEClassCreator.Models;
using UEClassCreator.Services;

namespace UEClassCreator.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private static readonly string[] PinnedClassNames =
    [
        "AActor", "APawn", "ACharacter", "AGameModeBase", "APlayerController",
        "UActorComponent", "USceneComponent", "UObject", "UUserWidget",
        "UGameInstance", "UGameInstanceSubsystem", "UWorldSubsystem"
    ];

    private readonly EngineLocator _engineLocator;
    private readonly HeaderScanner _scanner;
    private readonly ClassCache _cache;
    private readonly ClassFileGenerator _generator;

    private ClassIndex? _index;
    private CancellationTokenSource? _searchCts;
    private CancellationTokenSource? _scanCts;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateClassCommand))]
    private ClassEntry? _selectedClass;

    [ObservableProperty]
    private ClassDetailViewModel? _classDetail;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateClassCommand))]
    private bool _isBusy;

    [ObservableProperty]
    private int _scanProgress;

    [ObservableProperty]
    private int _scanTotal;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateClassCommand))]
    private string _newClassName = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateClassCommand))]
    private string _outputPath = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private EngineInstall? _selectedEngine;

    public ObservableCollection<ClassEntry> FilteredResults { get; } = [];
    public ObservableCollection<EngineInstall> AvailableEngines { get; } = [];

    // Set by the View so the ViewModel stays UI-framework-agnostic
    public Func<string?>? RequestFolderPick { get; set; }

    public MainViewModel(
        EngineLocator? engineLocator = null,
        HeaderScanner? scanner = null,
        ClassCache? cache = null,
        ClassFileGenerator? generator = null)
    {
        _engineLocator = engineLocator ?? new EngineLocator();
        _scanner = scanner ?? new HeaderScanner();
        _cache = cache ?? new ClassCache();
        _generator = generator ?? new ClassFileGenerator();
    }

    partial void OnSelectedClassChanged(ClassEntry? value)
    {
        ClassDetail = value is not null && _index is not null
            ? new ClassDetailViewModel(value, _index, entry => SelectedClass = entry)
            : null;
    }

    partial void OnSearchTextChanged(string value)
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var cts = _searchCts;
        _ = Task.Delay(200, cts.Token).ContinueWith(
            _ => UpdateFilteredResults(),
            cts.Token,
            TaskContinuationOptions.OnlyOnRanToCompletion,
            TaskScheduler.FromCurrentSynchronizationContext());
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        foreach (var engine in _engineLocator.FindInstalledEngines())
            AvailableEngines.Add(engine);

        if (AvailableEngines.Count == 0)
        {
            StatusMessage = "No Unreal Engine installation found.";
            return;
        }

        SelectedEngine = AvailableEngines[0];
        await LoadEngineAsync(SelectedEngine, forceRescan: false);
    }

    [RelayCommand]
    private async Task RescanAsync()
    {
        if (SelectedEngine is null) return;
        await LoadEngineAsync(SelectedEngine, forceRescan: true);
    }

    [RelayCommand]
    private void SelectClass(ClassEntry entry)
    {
        SelectedClass = entry;
    }

    [RelayCommand]
    private void BrowseOutputPath()
    {
        string? folder = RequestFolderPick?.Invoke();
        if (folder is not null)
            OutputPath = folder;
    }

    [RelayCommand(CanExecute = nameof(CanCreateClass))]
    private async Task CreateClassAsync()
    {
        if (SelectedClass is null) return;

        IsBusy = true;
        StatusMessage = "Creating files...";

        try
        {
            var request = new GenerationRequest(
                NewClassName,
                Description,
                OutputPath,
                SelectedClass,
                ProjectName: Path.GetFileName(OutputPath.TrimEnd(Path.DirectorySeparatorChar)),
                CompanyName: string.Empty);

            await _generator.GenerateAsync(request);

            string fileName = ClassFileGenerator.GetFileName(NewClassName);
            StatusMessage = $"Created {fileName} in {OutputPath}";
            NewClassName = string.Empty;
            Description = string.Empty;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanCreateClass() =>
        !string.IsNullOrWhiteSpace(NewClassName) &&
        SelectedClass is not null &&
        !string.IsNullOrWhiteSpace(OutputPath) &&
        !IsBusy;

    private async Task LoadEngineAsync(EngineInstall engine, bool forceRescan)
    {
        _scanCts?.Cancel();
        _scanCts = new CancellationTokenSource();

        IsBusy = true;
        ScanProgress = 0;
        ScanTotal = 0;

        try
        {
            List<ClassEntry> entries;

            if (!forceRescan && _cache.TryLoad(engine, out entries))
            {
                StatusMessage = $"Loaded {entries.Count:N0} classes from cache.";
            }
            else
            {
                entries = await ScanEngineAsync(engine, _scanCts.Token);
                if (!_scanCts.Token.IsCancellationRequested)
                    _cache.Save(engine, entries);
            }

            _index = new ClassIndex(entries);
            UpdateFilteredResults();
        }
        catch (OperationCanceledException) { }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task<List<ClassEntry>> ScanEngineAsync(EngineInstall engine, CancellationToken ct)
    {
        var searchDirs = new[]
        {
            Path.Combine(engine.Path, "Engine", "Source"),
            Path.Combine(engine.Path, "Engine", "Plugins"),
        };

        var files = searchDirs
            .Where(Directory.Exists)
            .SelectMany(d => Directory.GetFiles(d, "*.h", SearchOption.AllDirectories))
            .ToList();

        ScanTotal = files.Count;
        StatusMessage = $"Scanning {ScanTotal:N0} files...";

        var progress = new Progress<int>(n =>
        {
            ScanProgress = n;
            StatusMessage = $"Scanning... {n:N0} / {ScanTotal:N0}";
        });

        var entries = await _scanner.ScanAsync(files, engine.Source, progress, ct);
        StatusMessage = $"Found {entries.Count:N0} classes.";
        return entries;
    }

    private void UpdateFilteredResults()
    {
        FilteredResults.Clear();

        if (_index is null) return;

        IEnumerable<ClassEntry> results;

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            var pinnedSet = new HashSet<string>(PinnedClassNames);
            var pinned = PinnedClassNames
                .Select(name => _index.All.FirstOrDefault(e => e.ClassName == name))
                .OfType<ClassEntry>();
            var rest = _index.All.Where(e => !pinnedSet.Contains(e.ClassName));
            results = pinned.Concat(rest);
        }
        else
        {
            results = _index.Search(SearchText);
        }

        foreach (var entry in results)
            FilteredResults.Add(entry);
    }
}
