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
        Log("AutoValentia está listo. Selecciona uno o varios archivos .mat.");
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

        var added = 0;
        foreach (var file in files)
        {
            var localPath = file.TryGetLocalPath();
            if (localPath is null || _selectedFiles.Any(file =>
                    string.Equals(file.Path, localPath, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            _selectedFiles.Add(new PreparedMatFile(localPath));
            added++;
        }

        if (added > 0)
        {
            RefreshFiles();
            Log($"Se añadieron {added} archivo(s) .mat.");
        }
    }

    private void MoveUpButton_Click(object? sender, RoutedEventArgs eventArgs) => MoveSelectedFile(-1);

    private void MoveDownButton_Click(object? sender, RoutedEventArgs eventArgs) => MoveSelectedFile(1);

    private void RemoveButton_Click(object? sender, RoutedEventArgs eventArgs)
    {
        var selectedIndex = FilesListBox.SelectedIndex;
        if (selectedIndex < 0)
        {
            return;
        }

        var name = Path.GetFileName(_selectedFiles[selectedIndex].Path);
        _selectedFiles.RemoveAt(selectedIndex);
        RefreshFiles(Math.Min(selectedIndex, _selectedFiles.Count - 1));
        Log($"Se quitó {name} de la lista.");
    }

    private void FilesListBox_SelectionChanged(object? sender, SelectionChangedEventArgs eventArgs) => UpdateButtons();

    private async void ReviewMatricesButton_Click(object? sender, RoutedEventArgs eventArgs)
    {
        if (_selectedFiles.Count == 0)
        {
            return;
        }

        ReviewMatricesButton.IsEnabled = false;
        StatusText.Text = "Revisando matrices…";
        var reviewedFiles = await Task.Run(() => _selectedFiles
            .Select(file => MatFileReader.ReadAndNormalize(file.Path))
            .ToList());

        _selectedFiles.Clear();
        _selectedFiles.AddRange(reviewedFiles);
        RefreshFiles();

        var validCount = _selectedFiles.Count(file => file.IsReady);
        var convertedCount = _selectedFiles.Count(file => file.SourceColumnCount == 9 && file.IsReady);
        var errorCount = _selectedFiles.Count - validCount;
        StatusText.Text = $"{validCount} matriz(ces) preparada(s); {convertedCount} convertida(s) de 9 a 8 columnas; {errorCount} con error.";

        foreach (var file in _selectedFiles)
        {
            Log(file.ToString());
        }
    }

    private async void ExportReviewButton_Click(object? sender, RoutedEventArgs eventArgs)
    {
        if (_selectedFiles.Any(file => !file.IsReady))
        {
            StatusText.Text = "Revisa todas las matrices antes de crear el Excel.";
            return;
        }

        var invalidNames = _selectedFiles
            .Where(file => !SessionFileName.TryParse(file.Path, out _))
            .Select(file => Path.GetFileName(file.Path))
            .ToList();
        if (invalidNames.Count > 0)
        {
            StatusText.Text = "Hay nombres que no incluyen dNúmero rNúmero.";
            Log($"No se pudo exportar: {string.Join(", ", invalidNames)}");
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
            Log($"Excel de revisión guardado en {path}");
        }
        catch (Exception exception)
        {
            StatusText.Text = $"No se pudo crear el Excel: {exception.Message}";
            Log(StatusText.Text);
        }
        finally
        {
            UpdateButtons();
        }
    }

    private void MoveSelectedFile(int offset)
    {
        var sourceIndex = FilesListBox.SelectedIndex;
        var destinationIndex = sourceIndex + offset;
        if (sourceIndex < 0 || destinationIndex < 0 || destinationIndex >= _selectedFiles.Count)
        {
            return;
        }

        (_selectedFiles[sourceIndex], _selectedFiles[destinationIndex]) =
            (_selectedFiles[destinationIndex], _selectedFiles[sourceIndex]);
        RefreshFiles(destinationIndex);
    }

    private void RefreshFiles(int selectedIndex = -1)
    {
        FilesListBox.ItemsSource = _selectedFiles.Select(file => file.ToString()).ToList();
        FilesListBox.SelectedIndex = selectedIndex;
        StatusText.Text = _selectedFiles.Count == 0
            ? "Aún no hay archivos seleccionados."
            : $"{_selectedFiles.Count} archivo(s) .mat listos para preparar.";
        UpdateButtons();
    }

    private void UpdateButtons()
    {
        var selectedIndex = FilesListBox.SelectedIndex;
        RemoveButton.IsEnabled = selectedIndex >= 0;
        MoveUpButton.IsEnabled = selectedIndex > 0;
        MoveDownButton.IsEnabled = selectedIndex >= 0 && selectedIndex < _selectedFiles.Count - 1;
        ReviewMatricesButton.IsEnabled = _selectedFiles.Count > 0;
        ExportReviewButton.IsEnabled = _selectedFiles.Count > 0 && _selectedFiles.All(file => file.IsReady);
    }

    private void Log(string message)
    {
        LogTextBox.Text += $"{DateTime.Now:HH:mm:ss}  {message}{Environment.NewLine}";
        LogTextBox.CaretIndex = LogTextBox.Text?.Length ?? 0;
    }
}
