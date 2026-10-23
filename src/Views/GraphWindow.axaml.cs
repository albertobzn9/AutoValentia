using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ScottPlot;
using ScottPlot.TickGenerators;

namespace AutoValentia;

public partial class GraphWindow : Window
{
    private readonly GraphAnalysis? _analysis;

    public GraphWindow()
    {
        InitializeComponent();
    }

    public GraphWindow(GraphAnalysis analysis)
        : this()
    {
        _analysis = analysis;
        ConfigurePlot();
    }

    private async void ExportImageButton_Click(object? sender, RoutedEventArgs eventArgs)
    {
        var destination = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Guardar gráfica como imagen",
            SuggestedFileName = $"latencias-{GetFileNameSuffix()}.png",
            DefaultExtension = "png",
            FileTypeChoices =
            [
                new FilePickerFileType("Imagen PNG") { Patterns = ["*.png"] }
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
            ExportStatusText.Text = "No se pudo obtener una ubicación local para guardar la imagen.";
            return;
        }

        if (!string.Equals(Path.GetExtension(path), ".png", StringComparison.OrdinalIgnoreCase))
        {
            path += ".png";
        }

        try
        {
            // ScottPlot conserva los tamaños en píxeles al exportar. Se vuelve a
            // configurar temporalmente para que el PNG tenga textos y trazos proporcionales.
            ConfigurePlot(forExport: true);
            LatencyPlot.Plot.SavePng(path, 1600, 1000);
            ExportStatusText.Text = $"Imagen PNG guardada: {Path.GetFileName(path)}";
        }
        catch (Exception exception)
        {
            ExportStatusText.Text = $"No se pudo guardar la imagen: {exception.Message}";
        }
        finally
        {
            ConfigurePlot();
        }
    }

    private void ConfigurePlot(bool forExport = false)
    {
        var analysis = _analysis ?? throw new InvalidOperationException("No hay datos para graficar.");

        TitleText.Text = $"Latencias: {analysis.CrossingType.DisplayName()}";
        SubtitleText.Text = "Cada punto es el promedio de las ratas del día; los tres puntos corresponden a los tercios cronológicos del protocolo.";

        var plot = LatencyPlot.Plot;
        plot.Clear();

        var days = analysis.Points
            .Select(point => point.Day)
            .Distinct()
            .OrderBy(day => day)
            .ToList();

        for (var dayIndex = 0; dayIndex < days.Count; dayIndex++)
        {
            AddConditionForDay(plot, analysis, days[dayIndex], dayIndex, GraphCondition.Safe, Colors.Green, forExport);
            AddConditionForDay(plot, analysis, days[dayIndex], dayIndex, GraphCondition.Danger, Colors.Black, forExport);
        }

        var tickPositions = days.Select((_, index) => index * 4d + 2d).ToArray();
        var tickLabels = days.Select(day => $"d{day:0.##}").ToArray();
        var yMaximum = GetYMaximum(analysis.CrossingType);
        var yTickPositions = Enumerable.Range(0, (int)(yMaximum / 20d) + 1)
            .Select(index => index * 20d)
            .ToArray();
        var yTickLabels = yTickPositions.Select(value => value.ToString("0")).ToArray();

        plot.Axes.Bottom.TickGenerator = new NumericManual(tickPositions, tickLabels);
        plot.Axes.Left.TickGenerator = new NumericManual(yTickPositions, yTickLabels);
        plot.Axes.Bottom.Label.Text = "Días (bloques de ensayo)";
        plot.Axes.Left.Label.Text = "Latencias (s)";
        plot.Axes.SetLimitsY(0, yMaximum);
        plot.Axes.SetLimitsX(0.25, Math.Max(3.75, days.Count * 4d - 0.25));
        plot.Title(analysis.CrossingType.DisplayName());
        ConfigurePresentation(plot, forExport);
        plot.HideGrid();
        LatencyPlot.Refresh();
    }

    private static void ConfigurePresentation(Plot plot, bool forExport)
    {
        var tickFontSize = forExport ? 20 : 14;
        var axisFontSize = forExport ? 24 : 16;
        var titleFontSize = forExport ? 28 : 20;
        var legendFontSize = forExport ? 20 : 14;
        var lineWidth = forExport ? 3 : 2;

        // Helvetica (el valor predeterminado en macOS) tiene métricas que
        // desplazan los glifos fuera del área de recorte en ScottPlot 5.1.59.
        // Arial conserva el texto completo tanto en pantalla como al exportar.
        plot.Axes.Bottom.TickLabelStyle.FontName = "Arial";
        plot.Axes.Left.TickLabelStyle.FontName = "Arial";
        plot.Axes.Bottom.Label.FontName = "Arial";
        plot.Axes.Left.Label.FontName = "Arial";
        plot.Axes.Title.Label.FontName = "Arial";
        plot.Axes.Bottom.TickLabelStyle.FontSize = tickFontSize;
        plot.Axes.Left.TickLabelStyle.FontSize = tickFontSize;
        plot.Axes.Bottom.Label.FontSize = axisFontSize;
        plot.Axes.Left.Label.FontSize = axisFontSize;
        plot.Axes.Title.Label.FontSize = titleFontSize;

        // Reemplaza los elementos de la misma leyenda en cada exportación.
        plot.ShowLegend(CreateLegendItems(lineWidth), Alignment.UpperLeft);
        plot.Legend.FontName = "Arial";
        plot.Legend.FontSize = legendFontSize;
        plot.Legend.SymbolWidth = forExport ? 36 : 24;
        plot.Legend.SymbolHeight = forExport ? 20 : 14;
        plot.Legend.SymbolPadding = forExport ? 10 : 7;
        plot.Legend.Margin = new PixelPadding(forExport ? 16 : 10);
        plot.Legend.Padding = new PixelPadding(forExport ? 18 : 12);
    }

    private static LegendItem[] CreateLegendItems(float lineWidth) =>
    [
        new LegendItem
        {
            LineColor = Colors.Green,
            MarkerFillColor = Colors.Green,
            MarkerLineColor = Colors.Green,
            MarkerShape = MarkerShape.FilledCircle,
            LineWidth = lineWidth,
            LabelText = "Cruces seguros"
        },
        new LegendItem
        {
            LineColor = Colors.Black,
            MarkerFillColor = Colors.Black,
            MarkerLineColor = Colors.Black,
            MarkerShape = MarkerShape.FilledCircle,
            LineWidth = lineWidth,
            LabelText = "Cruces peligrosos"
        }
    ];

    private static void AddConditionForDay(Plot plot, GraphAnalysis analysis, decimal day, int dayIndex, GraphCondition condition, Color color, bool forExport)
    {
        var values = analysis.Points
            .Where(point => point.Day == day && point.Condition == condition && point.Mean is not null)
            .OrderBy(point => point.Block)
            .ToList();

        if (values.Count == 0)
        {
            return;
        }

        var xs = values.Select(point => dayIndex * 4d + point.Block).ToArray();
        var ys = values.Select(point => point.Mean!.Value).ToArray();
        var sems = values.Select(point => point.Sem ?? 0d).ToArray();

        var line = plot.Add.Scatter(xs, ys);
        line.Color = color;
        line.MarkerStyle.FillColor = color;
        line.MarkerStyle.Size = forExport ? 13 : 8;
        line.LineStyle.Width = forExport ? 3 : 2;

        var errors = plot.Add.ErrorBar(xs, ys, sems);
        errors.Color = color;
        errors.LineWidth = forExport ? 3 : 1.5f;
        errors.CapSize = forExport ? 10 : 6;
    }

    private static double GetYMaximum(CrossingType crossingType) => crossingType switch
    {
        CrossingType.SafeCrossings => 110d,
        CrossingType.DangerCrossings => 130d,
        CrossingType.Discrimination => 190d,
        _ => throw new ArgumentOutOfRangeException(nameof(crossingType))
    };

    private string GetFileNameSuffix() => _analysis?.CrossingType switch
    {
        CrossingType.SafeCrossings => "cruces-seguros",
        CrossingType.DangerCrossings => "cruces-peligrosos",
        CrossingType.Discrimination => "discriminacion",
        _ => "latencias"
    };
}
