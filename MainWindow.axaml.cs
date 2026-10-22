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
        StatusText.Text = "Aún no hay archivos seleccionados.";
        UpdateButtons();
    }

    private async void SelectFilesButton_Click(object? sender, RoutedEventArgs eventArgs)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Seleccionar archivos MATLAB",
            AllowMultiple = true,
            FileTypeFilter =
            [
                new FilePickerFileType("Archivos MATLAB") { Patterns = ["*.mat"] }
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
        StatusText.Text = "Preparando matrices…";
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
            StatusText.Text = "Hay archivos con error. Quítalos de la lista antes de crear el Excel.";
            return;
        }

        var invalidNames = _selectedFiles
            .Where(file => !SessionFileName.TryParse(file.Path, out _))
            .Select(file => Path.GetFileName(file.Path))
            .ToList();
        if (invalidNames.Count > 0)
        {
            StatusText.Text = "Hay nombres que no incluyen dNúmero rNúmero.";
            return;
        }

        var destination = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Guardar Excel de revisión",
            SuggestedFileName = "revision_latencias.xlsx",
            DefaultExtension = "xlsx",
            FileTypeChoices =
            [
                new FilePickerFileType("Archivo de Excel") { Patterns = ["*.xlsx"] }
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
            StatusText.Text = "No se pudo obtener una ubicación local para guardar el Excel.";
            return;
        }

        ExportReviewButton.IsEnabled = false;
        StatusText.Text = "Creando Excel de revisión…";
        try
        {
            var result = await Task.Run(() => ReviewExcelWriter.Write(path, _selectedFiles));
            StatusText.Text = $"Excel creado: {result.DayCount} día(s), {result.RatCount} rata(s).";
        }
        catch (Exception exception)
        {
            StatusText.Text = $"No se pudo crear el Excel: {exception.Message}";
        }
        finally
        {
            UpdateButtons();
        }
    }

    private void RefreshFiles(int selectedIndex = -1)
    {
        FilesListBox.ItemsSource = _selectedFiles.Select(file => file.ToString()).ToList();
        FilesListBox.SelectedIndex = selectedIndex;
        var readyCount = _selectedFiles.Count(file => file.IsReady);
        var errorCount = _selectedFiles.Count - readyCount;
        StatusText.Text = _selectedFiles.Count == 0
            ? "Aún no hay archivos seleccionados."
            : errorCount == 0
                ? $"{readyCount} archivo(s) preparado(s) y listo(s) para el Excel."
                : $"{readyCount} archivo(s) preparado(s); {errorCount} con error.";
        UpdateButtons();
    }

    private void UpdateButtons()
    {
        var selectedIndex = FilesListBox.SelectedIndex;
        SelectFilesButton.IsEnabled = true;
        RemoveButton.IsEnabled = selectedIndex >= 0;
        ExportReviewButton.IsEnabled = _selectedFiles.Count > 0 && _selectedFiles.All(file => file.IsReady);
    }
}
