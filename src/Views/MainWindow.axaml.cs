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
        StatusText.Text = "No files selected yet.";
        UpdateButtons();
    }

    private async void SelectFilesButton_Click(object? sender, RoutedEventArgs eventArgs)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select MATLAB files",
            AllowMultiple = true,
            FileTypeFilter =
            [
                new FilePickerFileType("MATLAB files") { Patterns = ["*.mat"] }
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
        StatusText.Text = "Preparing matrices…";
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
            StatusText.Text = "Some files contain errors. Remove them before creating the workbook.";
            return;
        }

        var invalidNames = _selectedFiles
            .Where(file => !SessionFileName.TryParse(file.Path, out _))
            .Select(file => Path.GetFileName(file.Path))
            .ToList();
        if (invalidNames.Count > 0)
        {
            StatusText.Text = "Some file names do not include dNumber rNumber.";
            return;
        }

        var destination = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save review workbook",
            SuggestedFileName = "latency_review.xlsx",
            DefaultExtension = "xlsx",
            FileTypeChoices =
            [
                new FilePickerFileType("Excel workbook") { Patterns = ["*.xlsx"] }
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
            StatusText.Text = "Could not get a local path to save the workbook.";
            return;
        }

        ExportReviewButton.IsEnabled = false;
        StatusText.Text = "Creating review workbook…";
        try
        {
            var result = await Task.Run(() => ReviewExcelWriter.Write(path, _selectedFiles));
            StatusText.Text = $"Workbook created: {result.CrossingType.DisplayName()}; {result.DayCount} day(s), {result.RatCount} rat(s).";
        }
        catch (Exception exception)
        {
            StatusText.Text = $"Could not create the workbook: {exception.Message}";
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
        }
        catch (Exception exception)
        {
            StatusText.Text = $"Could not create the plots: {exception.Message}";
        }
    }

    private void RefreshFiles(int selectedIndex = -1)
    {
        FilesListBox.ItemsSource = _selectedFiles.Select(file => file.ToString()).ToList();
        FilesListBox.SelectedIndex = selectedIndex;
        var readyCount = _selectedFiles.Count(file => file.IsReady);
        var errorCount = _selectedFiles.Count - readyCount;
        StatusText.Text = _selectedFiles.Count == 0
            ? "No files selected yet."
            : errorCount == 0
                ? $"{readyCount} file(s) prepared and ready for the workbook."
                : $"{readyCount} file(s) prepared; {errorCount} with errors.";
        UpdateButtons();
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
