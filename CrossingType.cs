namespace AutoValentia;

public enum CrossingType
{
    SafeCrossings,
    DangerCrossings,
    Discrimination
}

internal static class CrossingTypeExtensions
{
    public static string DisplayName(this CrossingType crossingType) => crossingType switch
    {
        CrossingType.SafeCrossings => "Cruces seguros",
        CrossingType.DangerCrossings => "Cruces peligrosos",
        CrossingType.Discrimination => "Discriminación",
        _ => throw new ArgumentOutOfRangeException(nameof(crossingType))
    };
}

internal static class CrossingTypeDetector
{
    public static CrossingType Detect(IReadOnlyCollection<RatLatencyReview> reviews)
    {
        var hasSafe = reviews.Any(review => review.SafeLatencies.Count > 0);
        var hasRisk = reviews.Any(review => review.RiskLatencies.Count > 0);

        return (hasSafe, hasRisk) switch
        {
            (true, false) => CrossingType.SafeCrossings,
            (false, true) => CrossingType.DangerCrossings,
            (true, true) => CrossingType.Discrimination,
            _ => throw new InvalidOperationException("No hay ensayos con desplazamiento válido para analizar.")
        };
    }
}
