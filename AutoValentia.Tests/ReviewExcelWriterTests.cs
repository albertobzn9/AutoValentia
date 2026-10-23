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
                CreateFile("exp_0526_dis_d1r1.mat", (0, 1.5d), (0, 2d), (0, 3d), (0, 4d), (0, 5d), (1, 10d), (1, 20d)),
                CreateFile("exp_0526_dis_d1r2.mat", (0, 6d), (0, 7d), (0, 8d)),
                CreateFile("exp_0526_dis_d2r1.mat", (0, 9d), (1, 30d), (1, 40d), (1, 50d))
            };

            ReviewExcelWriter.Write(outputPath, files);

            using var workbook = new XLWorkbook(outputPath);
            Assert.Equal(new[] { "Resumen", "d1", "d2" }, workbook.Worksheets.Select(sheet => sheet.Name));

            var summary = workbook.Worksheet("Resumen");
            Assert.Equal("exp_0526_dis", summary.Cell("A1").GetString());
            Assert.Equal("Discriminación", summary.Cell("C1").GetString());
            Assert.Equal("d1r1", summary.Cell("A2").GetString());
            Assert.Equal("d2r1", summary.Cell("C2").GetString());
            Assert.Equal("d1r2", summary.Cell("A6").GetString());
            Assert.False(summary.Cell("A3").Style.Font.Bold);
            Assert.Equal("0.00", summary.Cell("A3").Style.NumberFormat.Format);

            var dayOne = workbook.Worksheet("d1");
            Assert.Equal("d1r1", dayOne.Cell("A2").GetString());
            Assert.Equal("d1r2", dayOne.Cell("M2").GetString());
            Assert.True(dayOne.Cell("K3").Style.Font.Bold);
            Assert.True(dayOne.Cell("L3").Style.Font.Bold);
            Assert.False(dayOne.Cell("A2").Style.Font.Bold);
            Assert.Equal("0", dayOne.Cell("A3").Style.NumberFormat.Format);
            Assert.Equal("0.00", dayOne.Cell("D3").Style.NumberFormat.Format);

            // Each condition uses its own vertical thirds: values, bold average, then a blank row.
            Assert.Equal(1.5d, dayOne.Cell("I3").GetDouble());
            Assert.Equal("0.00", dayOne.Cell("I3").Style.NumberFormat.Format);
            Assert.Equal(2d, dayOne.Cell("I4").GetDouble());
            Assert.Equal(1.75d, dayOne.Cell("I5").GetDouble());
            Assert.True(dayOne.Cell("I5").Style.Font.Bold);
            Assert.True(dayOne.Cell("I6").IsEmpty());
            Assert.Equal(3d, dayOne.Cell("I7").GetDouble());
            Assert.Equal(3.5d, dayOne.Cell("I9").GetDouble());
            Assert.True(dayOne.Cell("I9").Style.Font.Bold);

            Assert.Equal(10d, dayOne.Cell("J3").GetDouble());
            Assert.Equal(10d, dayOne.Cell("J4").GetDouble());
            Assert.True(dayOne.Cell("J4").Style.Font.Bold);
            Assert.True(dayOne.Cell("J5").IsEmpty());
            Assert.Equal(20d, dayOne.Cell("J6").GetDouble());
            Assert.Equal(20d, dayOne.Cell("J7").GetDouble());
            Assert.True(dayOne.Cell("J7").Style.Font.Bold);
            Assert.True(dayOne.Cell("J8").IsEmpty());
            Assert.Equal("n/a", dayOne.Cell("J9").GetString());
            Assert.True(dayOne.Cell("J9").Style.Font.Bold);

            Assert.Equal("n/a", dayOne.Cell("V3").GetString());
            Assert.True(dayOne.Cell("V3").Style.Font.Bold);
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

    [Fact]
    public void PreparedMatFile_ready_description_hides_the_internal_normalization_details()
    {
        var matrix = Matrix<double>.Build.Dense(2, 8);
        var preparedFile = new PreparedMatFile("exp_0526_dis_d1r1.mat", matrix, SourceColumnCount: 9, RemovedRows: 2);

        Assert.DoesNotContain("columna", preparedFile.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("9", preparedFile.ToString());
        Assert.Equal("✓  exp_0526_dis_d1r1.mat — 2 filas preparadas", preparedFile.ToString());
    }

    [Fact]
    public void Processing_preserves_latency_values_and_rounds_the_final_block_average()
    {
        var block = new LatencyBlock(new[] { 1.234d, 2.344d }, []);

        Assert.Equal(1.234d, block.Safe[0]);
        Assert.Equal(1.79d, block.SafeAverage);
    }

    [Fact]
    public void Write_marks_the_safe_condition_as_not_applicable_for_danger_crossings()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"autovalentia-{Guid.NewGuid():N}.xlsx");
        try
        {
            var files = new[]
            {
                CreateFile("exp_0526_ame_d1r1.mat", (1, 10d), (1, 20d), (1, 30d), (1, 40d))
            };

            var result = ReviewExcelWriter.Write(outputPath, files);

            using var workbook = new XLWorkbook(outputPath);
            Assert.Equal(CrossingType.DangerCrossings, result.CrossingType);
            Assert.Equal("Cruces peligrosos", workbook.Worksheet("Resumen").Cell("C1").GetString());
            Assert.Equal("n/a", workbook.Worksheet("Resumen").Cell("A3").GetString());
            Assert.Equal("n/a", workbook.Worksheet("d1").Cell("I3").GetString());
            Assert.Equal("n/a", workbook.Worksheet("d1").Cell("K3").GetString());
            Assert.Equal(10d, workbook.Worksheet("d1").Cell("J3").GetDouble());
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public void Write_trims_the_final_discrimination_day_after_seven_safe_and_three_danger_trials()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"autovalentia-{Guid.NewGuid():N}.xlsx");
        try
        {
            var files = new[]
            {
                CreateFile("exp_0526_dis_d1r1.mat", (0, 1d), (1, 10d)),
                CreateFile(
                    "exp_0526_dis_d2r1.mat",
                    (0, 1d), (0, 2d), (0, 3d), (0, 4d), (0, 5d), (0, 6d), (0, 7d),
                    (1, 101d), (1, 102d), (1, 103d),
                    (0, 999d), (1, 999d))
            };

            var result = ReviewExcelWriter.Write(outputPath, files);

            using var workbook = new XLWorkbook(outputPath);
            var finalDay = workbook.Worksheet("d2");
            Assert.Equal(CrossingType.Discrimination, result.CrossingType);
            Assert.Equal(3d, finalDay.Cell("I5").GetDouble());
            Assert.Equal(2d, finalDay.Cell("I6").GetDouble());
            Assert.Equal(101d, finalDay.Cell("J3").GetDouble());
            Assert.Equal(101d, finalDay.Cell("J4").GetDouble());
            Assert.Equal(10d, finalDay.Cell("A12").GetDouble());
            Assert.True(finalDay.Cell("A13").IsEmpty());
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public void Write_excludes_danger_crossing_trials_after_the_protocol_latency_limit()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"autovalentia-{Guid.NewGuid():N}.xlsx");
        try
        {
            var files = new[]
            {
                CreateDangerFile("exp_0526_ame_d1r1.mat", (300d, 10d), (900d, 20d), (1d, 45.01d))
            };

            ReviewExcelWriter.Write(outputPath, files);

            using var workbook = new XLWorkbook(outputPath);
            var sheet = workbook.Worksheet("d1");
            Assert.Equal(10d, sheet.Cell("D3").GetDouble());
            Assert.Equal(20d, sheet.Cell("D4").GetDouble());
            Assert.Equal(300d, sheet.Cell("E3").GetDouble());
            Assert.Equal(900d, sheet.Cell("E4").GetDouble());
            Assert.True(sheet.Cell("A5").IsEmpty());
            Assert.Equal(10d, sheet.Cell("J3").GetDouble());
            Assert.Equal(20d, sheet.Cell("J6").GetDouble());
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

    private static PreparedMatFile CreateDangerFile(
        string fileName,
        params (double Time, double Latency)[] trials)
    {
        var matrix = Matrix<double>.Build.Dense(trials.Length, 8);
        for (var row = 0; row < trials.Length; row++)
        {
            matrix[row, 0] = row + 1;
            matrix[row, 2] = 1;
            matrix[row, 3] = trials[row].Latency;
            matrix[row, 4] = trials[row].Time;
            matrix[row, 7] = 2;
        }

        return new PreparedMatFile(fileName, matrix, SourceColumnCount: 8);
    }
}
