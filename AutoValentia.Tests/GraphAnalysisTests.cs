using MathNet.Numerics.LinearAlgebra;
using Xunit;

namespace AutoValentia.Tests;

public sealed class GraphAnalysisTests
{
    [Fact]
    public void Build_averages_each_rat_before_combining_them_and_marks_missing_safe_points_not_applicable()
    {
        var files = new[]
        {
            CreateFile("exp_0526_ame_d1r1.mat", 10d, 20d, 30d),
            CreateFile("exp_0526_ame_d1r2.mat", 20d, 40d, 60d),
            CreateFile("exp_0526_ame_d2r1.mat", 30d, 30d, 30d)
        };

        var analysis = GraphAnalysisBuilder.Build(files);

        Assert.Equal(CrossingType.DangerCrossings, analysis.CrossingType);
        Assert.Equal(12, analysis.Points.Count);

        var dayOneSafeFirstBlock = analysis.Points.Single(point =>
            point.Day == 1 && point.Block == 1 && point.Condition == GraphCondition.Safe);
        Assert.Null(dayOneSafeFirstBlock.Mean);
        Assert.Null(dayOneSafeFirstBlock.Sem);
        Assert.Equal(0, dayOneSafeFirstBlock.RatCount);

        var dayOneDangerFirstBlock = analysis.Points.Single(point =>
            point.Day == 1 && point.Block == 1 && point.Condition == GraphCondition.Danger);
        Assert.Equal(15d, dayOneDangerFirstBlock.Mean);
        Assert.Equal(5d, dayOneDangerFirstBlock.Sem);
        Assert.Equal(2, dayOneDangerFirstBlock.RatCount);

        var dayTwoDangerFirstBlock = analysis.Points.Single(point =>
            point.Day == 2 && point.Block == 1 && point.Condition == GraphCondition.Danger);
        Assert.Equal(30d, dayTwoDangerFirstBlock.Mean);
        Assert.Equal(0d, dayTwoDangerFirstBlock.Sem);
        Assert.Equal(1, dayTwoDangerFirstBlock.RatCount);
    }

    [Fact]
    public void Build_keeps_full_rat_precision_until_calculating_the_day_mean()
    {
        var files = new[]
        {
            CreateFile("exp_0526_dis_d1r1.mat", 1.234d, 1.234d, 1.234d),
            CreateFile("exp_0526_dis_d1r2.mat", 2.344d, 2.344d, 2.344d)
        };

        var analysis = GraphAnalysisBuilder.Build(files);
        var point = analysis.Points.Single(graphPoint =>
            graphPoint.Day == 1 && graphPoint.Block == 1 && graphPoint.Condition == GraphCondition.Danger);

        Assert.Equal(1.79d, point.Mean);
    }

    private static PreparedMatFile CreateFile(string fileName, params double[] latencies)
    {
        var matrix = Matrix<double>.Build.Dense(latencies.Length, 8);
        for (var row = 0; row < latencies.Length; row++)
        {
            matrix[row, 0] = row + 1;
            matrix[row, 2] = 1;
            matrix[row, 3] = latencies[row];
            matrix[row, 7] = 2;
        }

        return new PreparedMatFile(fileName, matrix, SourceColumnCount: 8);
    }
}
