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
    IReadOnlyList<GraphPoint> Points);

internal static class GraphAnalysisBuilder
{
    public static GraphAnalysis Build(IReadOnlyCollection<PreparedMatFile> files)
    {
        var reviews = files
            .Select(LatencyReviewBuilder.Build)
            .OrderBy(review => review.Identifier.Day)
            .ThenBy(review => review.Identifier.Rat)
            .ToList();

        if (reviews.Count == 0)
        {
            throw new InvalidOperationException("No hay matrices listas para analizar.");
        }

        var duplicate = reviews.GroupBy(review => review.Identifier).FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidOperationException($"Hay más de un archivo identificado como {duplicate.Key.DisplayName}.");
        }

        var crossingType = CrossingTypeDetector.Detect(reviews);
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

        return new GraphAnalysis(crossingType, points);
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
