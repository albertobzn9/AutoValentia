using System.Reflection;
using ScottPlot;
using Xunit;

namespace AutoValentia.Tests;

public sealed class GraphPresentationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Legend_glyphs_fit_inside_clip_and_repeated_exports_do_not_add_legends(bool forExport)
    {
        using var plot = new Plot();
        var initialPanelCount = plot.Axes.GetPanels().Length;
        var configure = typeof(GraphWindow).GetMethod("ConfigurePresentation",
            BindingFlags.NonPublic | BindingFlags.Static)!;

        // Exercise the same screen/export style transitions as the save button.
        for (var attempt = 0; attempt < 4; attempt++)
        {
            configure.Invoke(null, [plot, !forExport]);
            configure.Invoke(null, [plot, forExport]);
            Assert.Equal(initialPanelCount, plot.Axes.GetPanels().Length);

            using var paint = Paint.NewDisposablePaint();
            var items = plot.Legend.GetItems();
            Assert.Equal(2, items.Length);
            var layout = plot.Legend.GetLayout(new PixelSize(700, 400), paint);
            var clip = layout.LegendRect.Contract(plot.Legend.Padding).Expand(1f);

            for (var index = 0; index < items.Length; index++)
            {
                var label = items[index].LabelStyle;
                var measured = label.Measure(paint);
                label.ApplyToPaint(paint);
                paint.SKFont.MeasureText(label.Text, out var glyphBounds);

                // ScottPlot anchors the baseline at the label's bottom minus
                // the font's Bottom metric. Check actual glyphs, not just the box.
                var baseline = layout.LabelRects[index].LeftCenter.Y
                    + measured.Size.Height / 2 - measured.Bottom;
                Assert.True(baseline + glyphBounds.Top >= clip.Top,
                    $"The top of '{label.Text}' is clipped.");
                Assert.True(baseline + glyphBounds.Bottom <= clip.Bottom,
                    $"The bottom of '{label.Text}' is clipped.");
            }
        }
    }
}
