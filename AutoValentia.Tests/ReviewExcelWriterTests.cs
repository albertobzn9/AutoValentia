using ClosedXML.Excel;
using MathNet.Numerics.LinearAlgebra;
using Xunit;

namespace AutoValentia.Tests;

public sealed class ReviewExcelWriterTests
{
    [Fact]
    public void Write_uses_the_reference_layout_for_summary_and_daily_panels()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"autovalentia-{Guid.NewGuid():N}.xlsx");
        try
        {
            var files = new[]
            {
                CreateFile("exp_0526_dis_d1r1.mat", (0, 1d), (0, 2d), (0, 3d), (0, 4d), (0, 5d), (1, 10d), (1, 20d)),
                CreateFile("exp_0526_dis_d1r2.mat", (0, 6d), (0, 7d), (0, 8d)),
                CreateFile("exp_0526_dis_d2r1.mat", (0, 9d), (1, 30d), (1, 40d), (1, 50d))
            };

            ReviewExcelWriter.Write(outputPath, files);

            using var workbook = new XLWorkbook(outputPath);
            Assert.Equal(new[] { "Resumen", "d1", "d2" }, workbook.Worksheets.Select(sheet => sheet.Name));

            var summary = workbook.Worksheet("Resumen");
            Assert.Equal("exp_0526_dis", summary.Cell("A1").GetString());
            Assert.Equal("d1r1", summary.Cell("A2").GetString());
            Assert.Equal("d2r1", summary.Cell("C2").GetString());
            Assert.Equal("d1r2", summary.Cell("A6").GetString());
            Assert.False(summary.Cell("A3").Style.Font.Bold);

            var dayOne = workbook.Worksheet("d1");
            Assert.Equal("d1r1", dayOne.Cell("A2").GetString());
            Assert.Equal("d1r2", dayOne.Cell("M2").GetString());
            Assert.True(dayOne.Cell("K3").Style.Font.Bold);
            Assert.True(dayOne.Cell("L3").Style.Font.Bold);
            Assert.False(dayOne.Cell("A2").Style.Font.Bold);
            Assert.Equal("n/a", dayOne.Cell("X3").GetString());
            Assert.True(dayOne.Cell("X3").Style.Font.Bold);
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Theory]
    [InlineData("exp_0526_dis_d10.5r4.mat", "d10.5", "d10.5r4")]
    [InlineData("exp_0526_dis_d3r2.mat", "d3", "d3r2")]
    public void TryParse_accepts_day_identifiers(string fileName, string expectedSheetName, string expectedDisplayName)
    {
        var parsed = SessionFileName.TryParse(fileName, out var identifier);

        Assert.True(parsed);
        Assert.NotNull(identifier);
        Assert.Equal(expectedSheetName, identifier.DaySheetName);
        Assert.Equal(expectedDisplayName, identifier.DisplayName);
    }

    private static PreparedMatFile CreateFile(string fileName, params (int Stimulation, double Latency)[] trials)
    {
        var matrix = Matrix<double>.Build.Dense(trials.Length, 8);
        for (var row = 0; row < trials.Length; row++)
        {
            matrix[row, 0] = row + 1;
            matrix[row, 2] = trials[row].Stimulation;
            matrix[row, 3] = trials[row].Latency;
            matrix[row, 7] = 2;
        }

        return new PreparedMatFile(fileName, matrix, SourceColumnCount: 8);
    }
}
