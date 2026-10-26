namespace AutoValentia;

public enum GraphCondition
{
    Safe,
    Danger
}

public sealed record GraphPoint(
    decimal Day,
    int Block,
    GraphCondition Condition,
    double? Mean,
    double? Sem,
    int RatCount);

public sealed record GraphAnalysis(
    CrossingType CrossingType,
    IReadOnlyList<GraphPoint> Points,
    string? Warning = null);

internal static class GraphAnalysisBuilder
{
    public static GraphAnalysis Build(IReadOnlyCollection<PreparedMatFile> files)
    {
        var reviewSet = LatencyReviewSetBuilder.Build(files);
        var reviews = reviewSet.Reviews;
        var crossingType = reviewSet.CrossingType;
        var points = new List<GraphPoint>();

        foreach (var dayGroup in reviews.GroupBy(review => review.Identifier.Day).OrderBy(group => group.Key))
        {
            var dayReviews = dayGroup.ToList();
            for (var block = 0; block < 3; block++)
            {
                points.Add(BuildPoint(dayGroup.Key, block + 1, GraphCondition.Safe, dayReviews));
                points.Add(BuildPoint(dayGroup.Key, block + 1, GraphCondition.Danger, dayReviews));
            }
        }

        return new GraphAnalysis(crossingType, points, reviewSet.Warning);
    }

    private static GraphPoint BuildPoint(
        decimal day,
        int block,
        GraphCondition condition,
        IReadOnlyList<RatLatencyReview> reviews)
    {
        var ratAverages = reviews
            .Select(review => condition == GraphCondition.Safe
                ? review.Blocks[block - 1].SafeAverageRaw
                : review.Blocks[block - 1].RiskAverageRaw)
            .Where(average => average is not null)
            .Select(average => average!.Value)
            .ToList();

        if (ratAverages.Count == 0)
        {
            return new GraphPoint(day, block, condition, Mean: null, Sem: null, RatCount: 0);
        }

        var mean = Math.Round(ratAverages.Average(), 2, MidpointRounding.AwayFromZero);
        var sem = Math.Round(CalculateSem(ratAverages), 3, MidpointRounding.AwayFromZero);
        return new GraphPoint(day, block, condition, mean, sem, ratAverages.Count);
    }

    private static double CalculateSem(IReadOnlyList<double> values)
    {
        if (values.Count < 2)
        {
            return 0d;
        }

        var mean = values.Average();
        var sumOfSquares = values.Sum(value => Math.Pow(value - mean, 2));
        var sampleStandardDeviation = Math.Sqrt(sumOfSquares / (values.Count - 1));
        return sampleStandardDeviation / Math.Sqrt(values.Count);
    }
}
