namespace AutoValentia;

using MathNet.Numerics.LinearAlgebra;

internal sealed record LatencyReviewSet(
    IReadOnlyList<RatLatencyReview> Reviews,
    CrossingType CrossingType);

internal static class LatencyReviewSetBuilder
{
    public static LatencyReviewSet Build(IReadOnlyCollection<PreparedMatFile> files)
    {
        var initialReviews = files
            .Select(file => LatencyReviewBuilder.Build(file))
            .OrderBy(review => review.Identifier.Day)
            .ThenBy(review => review.Identifier.Rat)
            .ToList();

        if (initialReviews.Count == 0)
        {
            throw new InvalidOperationException("No matrices are ready for analysis.");
        }

        var duplicate = initialReviews.GroupBy(review => review.Identifier).FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidOperationException($"More than one file is identified as {duplicate.Key.DisplayName}.");
        }

        var crossingType = CrossingTypeDetector.Detect(initialReviews);
        if (crossingType == CrossingType.DangerCrossings)
        {
            var limitedReviews = initialReviews
                .Select(review =>
                {
                    var source = TrimDangerCrossingTrials(review.Source, review.Identifier.Day);
                    return LatencyReviewBuilder.Build(source);
                })
                .ToList();

            return new LatencyReviewSet(limitedReviews, crossingType);
        }

        if (crossingType != CrossingType.Discrimination)
        {
            return new LatencyReviewSet(initialReviews, crossingType);
        }

        var finalDay = initialReviews.Max(review => review.Identifier.Day);
        var reviews = initialReviews
            .Select(review => review.Identifier.Day == finalDay
                ? LatencyReviewBuilder.Build(review.Source, applyDiscriminationFinalDayLimit: true)
                : review)
            .ToList();

        return new LatencyReviewSet(reviews, crossingType);
    }

    private static PreparedMatFile TrimDangerCrossingTrials(PreparedMatFile source, decimal day)
    {
        var matrix = source.Matrix ?? throw new ArgumentException("The matrix is not ready for analysis.");
        var maximumLatency = GetDangerCrossingMaximumLatency(day);
        var acceptedRows = Enumerable.Range(0, matrix.RowCount)
            .Where(row => matrix[row, 3] <= maximumLatency)
            .ToList();

        if (acceptedRows.Count == matrix.RowCount)
        {
            return source;
        }

        var trimmed = Matrix<double>.Build.Dense(acceptedRows.Count, matrix.ColumnCount);
        for (var trimmedRow = 0; trimmedRow < acceptedRows.Count; trimmedRow++)
        {
            var sourceRow = acceptedRows[trimmedRow];
            for (var column = 0; column < matrix.ColumnCount; column++)
            {
                trimmed[trimmedRow, column] = matrix[sourceRow, column];
            }

            trimmed[trimmedRow, 0] = trimmedRow + 1;
        }

        return source with
        {
            Matrix = trimmed,
            RemovedRows = source.RemovedRows + matrix.RowCount - acceptedRows.Count
        };
    }

    private static double GetDangerCrossingMaximumLatency(decimal day) => day switch
    {
        1m or 2m => 30d + 15d,
        3m => 60d + 15d,
        4m => 90d + 15d,
        5m => 120d + 15d,
        _ => throw new InvalidOperationException(
            $"Conflict crossings only support days d1 through d5; received d{day:0.##}.")
    };
}
