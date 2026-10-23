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

    [Fact]
    public void Build_limits_only_the_final_discrimination_day_to_seven_safe_and_three_danger_trials()
    {
        var files = new[]
        {
            CreateDiscriminationFile(
                "exp_0526_dis_d1r1.mat",
                (0, 10d), (0, 11d), (0, 12d), (0, 13d), (0, 14d), (0, 15d), (0, 16d), (0, 17d),
                (1, 100d), (1, 101d), (1, 102d), (1, 103d)),
            CreateDiscriminationFile(
                "exp_0526_dis_d2r1.mat",
                (0, 1d), (0, 2d), (0, 3d), (0, 4d), (0, 5d), (0, 6d), (0, 7d),
                (1, 101d), (1, 102d), (1, 103d),
                (0, 999d), (1, 999d))
        };

        var analysis = GraphAnalysisBuilder.Build(files);

        Assert.Equal(CrossingType.Discrimination, analysis.CrossingType);
        Assert.Equal(16.5d, Point(analysis, 1, 3, GraphCondition.Safe).Mean);
        Assert.Equal(103d, Point(analysis, 1, 3, GraphCondition.Danger).Mean);
        Assert.Equal(2d, Point(analysis, 2, 1, GraphCondition.Safe).Mean);
        Assert.Equal(4.5d, Point(analysis, 2, 2, GraphCondition.Safe).Mean);
        Assert.Equal(6.5d, Point(analysis, 2, 3, GraphCondition.Safe).Mean);
        Assert.Equal(101d, Point(analysis, 2, 1, GraphCondition.Danger).Mean);
        Assert.Equal(102d, Point(analysis, 2, 2, GraphCondition.Danger).Mean);
        Assert.Equal(103d, Point(analysis, 2, 3, GraphCondition.Danger).Mean);
    }

    private static GraphPoint Point(GraphAnalysis analysis, decimal day, int block, GraphCondition condition) =>
        analysis.Points.Single(point => point.Day == day && point.Block == block && point.Condition == condition);

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

    private static PreparedMatFile CreateDiscriminationFile(
        string fileName,
        params (int Stimulation, double Latency)[] trials)
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
