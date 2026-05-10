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
    private readonly ProjectPersistence _projectPersistence;
    private readonly SettingsService _settingsService;
    private readonly AppSettings _settings;

    private ClassIndex? _index;
    private CancellationTokenSource? _searchCts;
    private CancellationTokenSource? _scanCts;
    private bool _suppressOutputPathSave;

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
    private string _statusMessage = "Add a project to get started.";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateClassCommand))]
    private string _newClassName = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateClassCommand))]
    private string _outputPath = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private UProjectEntry? _selectedProject;

    [ObservableProperty]
    private string _companyName = string.Empty;

    public ObservableCollection<ClassEntry> FilteredResults { get; } = [];
    public ObservableCollection<UProjectEntry> AvailableProjects { get; } = [];

    public Func<string?>? RequestFolderPick { get; set; }
    public Func<string?>? RequestProjectPick { get; set; }

    public MainViewModel(
        EngineLocator? engineLocator = null,
        HeaderScanner? scanner = null,
        ClassCache? cache = null,
        ClassFileGenerator? generator = null,
        ProjectPersistence? projectPersistence = null,
        SettingsService? settingsService = null)
    {
        _engineLocator     = engineLocator     ?? new EngineLocator();
        _scanner           = scanner           ?? new HeaderScanner();
        _cache             = cache             ?? new ClassCache();
        _generator         = generator         ?? new ClassFileGenerator();
        _projectPersistence = projectPersistence ?? new ProjectPersistence();
        _settingsService   = settingsService   ?? new SettingsService();

        _settings    = _settingsService.Load();
        _companyName = _settings.CompanyName;
    }

    partial void OnCompanyNameChanged(string value)
    {
        _settings.CompanyName = value;
        _settingsService.Save(_settings);
    }

    partial void OnOutputPathChanged(string value)
    {
        if (_suppressOutputPathSave || SelectedProject is null || string.IsNullOrEmpty(value)) return;
        _settings.LastOutputPaths[SelectedProject.UProjectPath] = value;
        _settingsService.Save(_settings);
    }

    partial void OnSelectedClassChanged(ClassEntry? value)
    {
        ClassDetail = value is not null && _index is not null
            ? new ClassDetailViewModel(value, _index, entry => SelectedClass = entry)
            : null;

        if (value is not null && SelectedProject is not null)
        {
            _settings.LastSelectedClasses[SelectedProject.UProjectPath] = value.ClassName;
            _settingsService.Save(_settings);
        }
    }

    partial void OnSelectedProjectChanged(UProjectEntry? value)
    {
        if (value is null) return;

        // Restore last output path for this project, or default to Source
        _suppressOutputPathSave = true;
        OutputPath = _settings.LastOutputPaths.TryGetValue(value.UProjectPath, out var saved) && !string.IsNullOrEmpty(saved)
            ? saved
            : Path.Combine(value.ProjectDirectory, "Source");
        _suppressOutputPathSave = false;

        _ = LoadProjectAsync(value, forceRescan: false);
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
        var savedPaths = _projectPersistence.Load();
        foreach (var path in savedPaths)
        {
            var entry = _engineLocator.ResolveForProject(path);
            if (entry is not null) AvailableProjects.Add(entry);
        }

        if (AvailableProjects.Count > 0)
            SelectedProject = AvailableProjects[0];

        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task AddProjectAsync()
    {
        string? path = RequestProjectPick?.Invoke();
        if (path is null) return;

        var entry = _engineLocator.ResolveForProject(path);
        if (entry is null)
        {
            StatusMessage = $"Could not resolve engine for {Path.GetFileName(path)}.";
            return;
        }

        if (!AvailableProjects.Any(p => p.UProjectPath.Equals(path, StringComparison.OrdinalIgnoreCase)))
        {
            AvailableProjects.Add(entry);
            _projectPersistence.Save(AvailableProjects.Select(p => p.UProjectPath));
        }

        SelectedProject = entry;
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task RescanAsync()
    {
        if (SelectedProject is null) return;
        await LoadProjectAsync(SelectedProject, forceRescan: true);
    }

    [RelayCommand]
    private void SelectClass(ClassEntry entry) => SelectedClass = entry;

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
        if (SelectedClass is null || SelectedProject is null) return;

        IsBusy = true;
        StatusMessage = "Creating files...";

        try
        {
            var request = new GenerationRequest(
                NewClassName,
                Description,
                OutputPath,
                SelectedClass,
                ProjectName: SelectedProject.ProjectName,
                CompanyName: _settings.CompanyName);

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

    private async Task LoadProjectAsync(UProjectEntry project, bool forceRescan)
    {
        _scanCts?.Cancel();
        _scanCts = new CancellationTokenSource();
        var ct = _scanCts.Token;

        IsBusy = true;
        ScanProgress = 0;
        ScanTotal = 0;
        _index = null;
        FilteredResults.Clear();

        try
        {
            List<ClassEntry> engineEntries;
            if (!forceRescan && _cache.TryLoad(project.EnginePath, project.EngineSource, out engineEntries))
            {
                StatusMessage = $"Loaded {engineEntries.Count:N0} engine classes from cache.";
            }
            else
            {
                engineEntries = await ScanDirectoriesAsync(GetEngineHeaderDirs(project), project.EngineSource, ct);
                if (!ct.IsCancellationRequested)
                    _cache.Save(project.EnginePath, engineEntries);
            }

            var projectEntries = await ScanDirectoriesAsync(GetProjectHeaderDirs(project), EngineSource.GameProject, ct);

            if (ct.IsCancellationRequested) return;

            _index = new ClassIndex(engineEntries.Concat(projectEntries));
            StatusMessage = $"{project.ProjectName} — {_index.All.Count:N0} classes ({projectEntries.Count} project)";
            UpdateFilteredResults();

            // Restore last selected class for this project
            if (_settings.LastSelectedClasses.TryGetValue(project.UProjectPath, out var lastClass))
                SelectedClass = _index.All.FirstOrDefault(e => e.ClassName == lastClass);
        }
        catch (OperationCanceledException) { }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task<List<ClassEntry>> ScanDirectoriesAsync(
        IEnumerable<string> dirs, EngineSource source, CancellationToken ct)
    {
        var files = dirs
            .Where(Directory.Exists)
            .SelectMany(d => Directory.GetFiles(d, "*.h", SearchOption.AllDirectories))
            .ToList();

        if (files.Count == 0) return [];

        ScanTotal += files.Count;
        StatusMessage = $"Scanning {ScanTotal:N0} files...";

        int before = ScanProgress;
        var progress = new Progress<int>(n =>
        {
            ScanProgress = before + n;
            StatusMessage = $"Scanning... {ScanProgress:N0} / {ScanTotal:N0}";
        });

        return await _scanner.ScanAsync(files, source, progress, ct);
    }

    private static IEnumerable<string> GetEngineHeaderDirs(UProjectEntry project) =>
    [
        Path.Combine(project.EnginePath, "Engine", "Source"),
        Path.Combine(project.EnginePath, "Engine", "Plugins"),
    ];

    private static IEnumerable<string> GetProjectHeaderDirs(UProjectEntry project) =>
    [
        Path.Combine(project.ProjectDirectory, "Source"),
        Path.Combine(project.ProjectDirectory, "Plugins"),
    ];

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
