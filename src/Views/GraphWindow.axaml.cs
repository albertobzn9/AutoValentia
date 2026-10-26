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
        AppText.LanguageChanged += ApplyLocalizedText;
        Closed += (_, _) => AppText.LanguageChanged -= ApplyLocalizedText;
        ApplyLocalizedText();
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
            Title = AppText.SavePlotTitle,
            SuggestedFileName = $"{AppText.PlotFileNamePrefix}-{GetFileNameSuffix()}.png",
            DefaultExtension = "png",
            FileTypeChoices =
            [
                new FilePickerFileType(AppText.PngImage) { Patterns = ["*.png"] }
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
            ExportStatusText.Text = AppText.NoLocalImagePath;
            return;
        }

        if (!string.Equals(Path.GetExtension(path), ".png", StringComparison.OrdinalIgnoreCase))
        {
            path += ".png";
        }

        try
        {
            // ScottPlot preserves pixel sizes when exporting. Configure it temporarily
            // so the PNG uses proportional text and line weights.
            ConfigurePlot(forExport: true);
            LatencyPlot.Plot.SavePng(path, 1600, 1000);
            ExportStatusText.Text = AppText.PngImageSaved(Path.GetFileName(path));
        }
        catch (Exception exception)
        {
            ExportStatusText.Text = AppText.CouldNotSaveImage(exception.Message);
        }
        finally
        {
            ConfigurePlot();
        }
    }

    private void ConfigurePlot(bool forExport = false)
    {
        var analysis = _analysis ?? throw new InvalidOperationException(AppText.NoDataToPlot);

        TitleText.Text = AppText.LatenciesTitle(analysis.CrossingType);
        SubtitleText.Text = AppText.PlotSubtitle;

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
        plot.Axes.Bottom.Label.Text = AppText.DayAxisLabel;
        plot.Axes.Left.Label.Text = AppText.LatencyAxisLabel;
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

        // Helvetica (the default on macOS) has metrics that shift glyphs outside
        // the clipping area in ScottPlot 5.1.59. Arial preserves full text both
        // on screen and in exported images.
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

        // Replaces the same legend items on every export.
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
            LabelText = AppText.SafeCrossings
        },
        new LegendItem
        {
            LineColor = Colors.Black,
            MarkerFillColor = Colors.Black,
            MarkerLineColor = Colors.Black,
            MarkerShape = MarkerShape.FilledCircle,
            LineWidth = lineWidth,
            LabelText = AppText.DangerCrossings
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
        CrossingType.SafeCrossings => AppText.CurrentLanguage == AppLanguage.Spanish ? "cruces-seguros" : "no-conflict-crossings",
        CrossingType.DangerCrossings => AppText.CurrentLanguage == AppLanguage.Spanish ? "cruces-peligrosos" : "conflict-crossings",
        CrossingType.Discrimination => "discrimination",
        _ => AppText.CurrentLanguage == AppLanguage.Spanish ? "latencias" : "latencies"
    };

    private void ApplyLocalizedText()
    {
        Title = AppText.LatencyPlotsTitle;
        SafeLegendText.Text = AppText.SafeCrossings;
        DangerLegendText.Text = AppText.DangerCrossings;
        SemDescriptionText.Text = AppText.SemDescription;
        ExportImageButton.Content = AppText.ExportPngButton;

        if (_analysis is not null)
        {
            ConfigurePlot();
        }
    }
}
