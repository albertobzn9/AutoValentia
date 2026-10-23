using Avalonia.Controls;
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

    private void ConfigurePlot()
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
            AddConditionForDay(plot, analysis, days[dayIndex], dayIndex, GraphCondition.Safe, Colors.Green);
            AddConditionForDay(plot, analysis, days[dayIndex], dayIndex, GraphCondition.Danger, Colors.Black);
        }

        var tickPositions = days.Select((_, index) => index * 4d + 2d).ToArray();
        var tickLabels = days.Select(day => $"d{day:0.##}").ToArray();
        plot.Axes.Bottom.TickGenerator = new NumericManual(tickPositions, tickLabels);
        plot.Axes.Bottom.Label.Text = "Días (bloques de ensayo)";
        plot.Axes.Left.Label.Text = "Latencias (s)";
        plot.Axes.SetLimitsY(0, GetYMaximum(analysis.CrossingType));
        plot.Axes.SetLimitsX(0.25, Math.Max(3.75, days.Count * 4d - 0.25));
        plot.Title(analysis.CrossingType.DisplayName());
        LatencyPlot.Refresh();
    }

    private static void AddConditionForDay(Plot plot, GraphAnalysis analysis, decimal day, int dayIndex, GraphCondition condition, Color color)
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
        line.MarkerStyle.Size = 9;
        line.LineStyle.Width = 2;

        var errors = plot.Add.ErrorBar(xs, ys, sems);
        errors.Color = color;
    }

    private static double GetYMaximum(CrossingType crossingType) => crossingType switch
    {
        CrossingType.SafeCrossings => 110d,
        CrossingType.DangerCrossings => 130d,
        CrossingType.Discrimination => 190d,
        _ => throw new ArgumentOutOfRangeException(nameof(crossingType))
    };
}
