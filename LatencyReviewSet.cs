namespace AutoValentia;

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
            throw new InvalidOperationException("No hay matrices listas para analizar.");
        }

        var duplicate = initialReviews.GroupBy(review => review.Identifier).FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidOperationException($"Hay más de un archivo identificado como {duplicate.Key.DisplayName}.");
        }

        var crossingType = CrossingTypeDetector.Detect(initialReviews);
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
}
