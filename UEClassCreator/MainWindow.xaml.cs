using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using UEClassCreator.Services;
using UEClassCreator.ViewModels;

namespace UEClassCreator
{
    public partial class MainWindow : Window
    {
        private MainViewModel ViewModel => (MainViewModel)DataContext;
        private readonly SettingsService _settingsService = new();

        public MainWindow()
        {
            InitializeComponent();

            var vm = new MainViewModel(settingsService: _settingsService);
            vm.RequestFolderPick  = PickFolder;
            vm.RequestProjectPick = PickProject;
            DataContext = vm;

            RestoreWindowGeometry();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _ = ViewModel.InitializeCommand.ExecuteAsync(null);
            SearchBox.Focus();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            SaveWindowGeometry();
        }

        private void RestoreWindowGeometry()
        {
            var settings = _settingsService.Load();
            Width  = settings.WindowWidth;
            Height = settings.WindowHeight;

            if (!double.IsNaN(settings.WindowLeft) && !double.IsNaN(settings.WindowTop))
            {
                Left = settings.WindowLeft;
                Top  = settings.WindowTop;
                WindowStartupLocation = WindowStartupLocation.Manual;
            }
        }

        private void SaveWindowGeometry()
        {
            var settings = _settingsService.Load();
            settings.WindowWidth  = Width;
            settings.WindowHeight = Height;
            settings.WindowLeft   = Left;
            settings.WindowTop    = Top;
            _settingsService.Save(settings);
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && ViewModel.FilteredResults.Count > 0)
            {
                ViewModel.SelectedClass = ViewModel.FilteredResults[0];
                ClassNameBox.Focus();
                e.Handled = true;
            }
        }

        private static string? PickFolder()
        {
            var dialog = new OpenFolderDialog { Title = "Select output folder" };
            return dialog.ShowDialog() == true ? dialog.FolderName : null;
        }

        private static string? PickProject()
        {
            var dialog = new OpenFileDialog
            {
                Title  = "Select Unreal project",
                Filter = "Unreal Project (*.uproject)|*.uproject",
            };
            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }
    }
}
