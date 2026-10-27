using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace AutoValentia;

public partial class MainWindow : Window
{
    private readonly List<PreparedMatFile> _selectedFiles = [];

    public MainWindow()
    {
        InitializeComponent();
        AppText.LanguageChanged += ApplyLocalizedText;
        Closed += (_, _) => AppText.LanguageChanged -= ApplyLocalizedText;
        ApplyLocalizedText();
        UpdateButtons();
    }

    private void LanguageToggleSwitch_IsCheckedChanged(object? sender, RoutedEventArgs eventArgs) =>
        AppText.SetLanguage(LanguageToggleSwitch.IsChecked == true ? AppLanguage.Spanish : AppLanguage.English);

    private void ApplyLocalizedText()
    {
        LanguageToggleSwitch.IsChecked = AppText.CurrentLanguage == AppLanguage.Spanish;
        SubtitleText.Text = AppText.MainSubtitle;
        SelectFilesHeadingText.Text = AppText.SelectFilesHeading;
        SelectFilesDescriptionText.Text = AppText.SelectFilesDescription;
        SelectFilesButton.Content = AppText.SelectMatFilesButton;
        RemoveButton.Content = AppText.RemoveFromListButton;
        ExportReviewButton.Content = AppText.CreateWorkbookButton;
        GraphButton.Content = AppText.ViewPlotsButton;
        RefreshFiles(FilesListBox.SelectedIndex);
    }

    private async void SelectFilesButton_Click(object? sender, RoutedEventArgs eventArgs)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = AppText.SelectMatFilesTitle,
            AllowMultiple = true,
            FileTypeFilter =
            [
                new FilePickerFileType(AppText.MatFiles) { Patterns = ["*.mat"] }
            ]
        });

        var pathsToPrepare = new List<string>();
        foreach (var file in files)
        {
            var localPath = file.TryGetLocalPath();
            if (localPath is null || _selectedFiles.Any(file =>
                    string.Equals(file.Path, localPath, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            pathsToPrepare.Add(localPath);
        }

        if (pathsToPrepare.Count == 0)
        {
            return;
        }

        SelectFilesButton.IsEnabled = false;
        StatusText.Text = AppText.PreparingMatrices;
        try
        {
            var preparedFiles = await Task.Run(() => pathsToPrepare
                .Select(MatFileReader.ReadAndNormalize)
                .ToList());
            _selectedFiles.AddRange(preparedFiles);
            RefreshFiles();
        }
        finally
        {
            UpdateButtons();
        }
    }

    private void RemoveButton_Click(object? sender, RoutedEventArgs eventArgs)
    {
        var selectedIndex = FilesListBox.SelectedIndex;
        if (selectedIndex < 0)
        {
            return;
        }

        _selectedFiles.RemoveAt(selectedIndex);
        RefreshFiles(Math.Min(selectedIndex, _selectedFiles.Count - 1));
    }

    private void FilesListBox_SelectionChanged(object? sender, SelectionChangedEventArgs eventArgs) => UpdateButtons();

    private async void ExportReviewButton_Click(object? sender, RoutedEventArgs eventArgs)
    {
        if (_selectedFiles.Any(file => !file.IsReady))
        {
            StatusText.Text = AppText.FilesContainErrors;
            return;
        }

        var invalidNames = _selectedFiles
            .Where(file => !SessionFileName.TryParse(file.Path, out _))
            .Select(file => Path.GetFileName(file.Path))
            .ToList();
        if (invalidNames.Count > 0)
        {
            StatusText.Text = AppText.InvalidFileNames;
            return;
        }

        var destination = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = AppText.SaveReviewWorkbookTitle,
            SuggestedFileName = AppText.ReviewWorkbookFileName,
            DefaultExtension = "xlsx",
            FileTypeChoices =
            [
                new FilePickerFileType(AppText.ExcelWorkbook) { Patterns = ["*.xlsx"] }
            ],
            ShowOverwritePrompt = true
        });
        if (destination is null)
        {
            return;
        }

        var path = destination.TryGetLocalPath();
        if (path is null)
        {
            StatusText.Text = AppText.NoLocalWorkbookPath;
            return;
        }

        ExportReviewButton.IsEnabled = false;
        StatusText.Text = AppText.CreatingWorkbook;
        try
        {
            var result = await Task.Run(() => ReviewExcelWriter.Write(path, _selectedFiles));
            StatusText.Text = AppText.WithWarning(
                AppText.WorkbookCreated(result.CrossingType, result.DayCount, result.RatCount),
                result.Warning);
        }
        catch (Exception exception)
        {
            StatusText.Text = AppText.CouldNotCreateWorkbook(exception.Message);
        }
        finally
        {
            UpdateButtons();
        }
    }

    private void GraphButton_Click(object? sender, RoutedEventArgs eventArgs)
    {
        try
        {
            var analysis = GraphAnalysisBuilder.Build(_selectedFiles);
            var graphWindow = new GraphWindow(analysis);
            graphWindow.Show(this);
            if (analysis.Warning is not null)
            {
                StatusText.Text = analysis.Warning;
            }
        }
        catch (Exception exception)
        {
            StatusText.Text = AppText.CouldNotCreatePlots(exception.Message);
        }
    }

    private void RefreshFiles(int selectedIndex = -1)
    {
        FilesListBox.ItemsSource = _selectedFiles.Select(file => file.ToString()).ToList();
        FilesListBox.SelectedIndex = selectedIndex;
        var readyCount = _selectedFiles.Count(file => file.IsReady);
        var errorCount = _selectedFiles.Count - readyCount;
        var status = _selectedFiles.Count == 0
            ? AppText.NoFilesSelected
            : AppText.PreparedFilesSummary(readyCount, errorCount);
        StatusText.Text = AppText.WithWarning(status, GetDiscriminationDayWarning());
        UpdateButtons();
    }

    private string? GetDiscriminationDayWarning()
    {
        if (_selectedFiles.Count == 0 || _selectedFiles.Any(file => !file.IsReady) ||
            _selectedFiles.Any(file => !SessionFileName.TryParse(file.Path, out _)))
        {
            return null;
        }

        try
        {
            return LatencyReviewSetBuilder.Build(_selectedFiles).Warning;
        }
        catch
        {
            return null;
        }
    }

    private void UpdateButtons()
    {
        var selectedIndex = FilesListBox.SelectedIndex;
        SelectFilesButton.IsEnabled = true;
        RemoveButton.IsEnabled = selectedIndex >= 0;
        ExportReviewButton.IsEnabled = _selectedFiles.Count > 0 && _selectedFiles.All(file => file.IsReady);
        GraphButton.IsEnabled = _selectedFiles.Count > 0 && _selectedFiles.All(file => file.IsReady);
    }
}
