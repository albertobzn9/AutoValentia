using MathNet.Numerics.LinearAlgebra;

namespace AutoValentia;

internal sealed record LatencyBlock(IReadOnlyList<double> Safe, IReadOnlyList<double> Risk)
{
    public double? SafeAverageRaw => Safe.Count == 0 ? null : Safe.Average();

    public double? RiskAverageRaw => Risk.Count == 0 ? null : Risk.Average();

    public double? SafeAverage => RoundForDisplay(SafeAverageRaw);

    public double? RiskAverage => RoundForDisplay(RiskAverageRaw);

    private static double? RoundForDisplay(double? value) =>
        value is null ? null : Math.Round(value.Value, 2, MidpointRounding.AwayFromZero);
}

internal sealed record RatLatencyReview(
    PreparedMatFile Source,
    SessionIdentifier Identifier,
    IReadOnlyList<double> SafeLatencies,
    IReadOnlyList<double> RiskLatencies,
    IReadOnlyList<LatencyBlock> Blocks,
    int MatrixRowsForExport);

internal static class LatencyReviewBuilder
{
    public static RatLatencyReview Build(PreparedMatFile file, bool applyDiscriminationFinalDayLimit = false)
    {
        if (!file.IsReady || file.Matrix is null)
        {
            throw new ArgumentException($"{Path.GetFileName(file.Path)} is not ready for analysis.");
        }

        if (!SessionFileName.TryParse(file.Path, out var identifier) || identifier is null)
        {
            throw new ArgumentException(
                $"{Path.GetFileName(file.Path)} must include the dNumber rNumber pattern, for example d3r4.");
        }

        var safe = new List<double>();
        var risk = new List<double>();
        var matrixRowsForExport = AddEligibleLatencies(
            file.Matrix,
            safe,
            risk,
            applyDiscriminationFinalDayLimit ? 7 : null,
            applyDiscriminationFinalDayLimit ? 3 : null);

        var safeBlocks = SplitIntoThree(safe);
        var riskBlocks = SplitIntoThree(risk);
        var blocks = Enumerable.Range(0, 3)
            .Select(index => new LatencyBlock(safeBlocks[index], riskBlocks[index]))
            .ToList();

        return new RatLatencyReview(file, identifier, safe, risk, blocks, matrixRowsForExport);
    }

    private static int AddEligibleLatencies(
        Matrix<double> matrix,
        ICollection<double> safe,
        ICollection<double> risk,
        int? maximumSafe,
        int? maximumRisk)
    {
        for (var row = 0; row < matrix.RowCount; row++)
        {
            if (matrix[row, 7] <= 1d)
            {
                continue;
            }

            var stimulation = matrix[row, 2];
            var latency = matrix[row, 3];
            if (stimulation == 0d && (!maximumSafe.HasValue || safe.Count < maximumSafe.Value))
            {
                safe.Add(latency);
            }
            else if (stimulation == 1d && (!maximumRisk.HasValue || risk.Count < maximumRisk.Value))
            {
                risk.Add(latency);
            }

            if (maximumSafe.HasValue && maximumRisk.HasValue &&
                safe.Count == maximumSafe.Value && risk.Count == maximumRisk.Value)
            {
                // The discrimination protocol ends at the tenth valid crossing:
                // 7 no-conflict and 3 conflict crossings. Later rows are not exported.
                return row + 1;
            }
        }

        return matrix.RowCount;
    }

    private static IReadOnlyList<IReadOnlyList<double>> SplitIntoThree(IReadOnlyList<double> values)
    {
        var baseSize = values.Count / 3;
        var remainder = values.Count % 3;
        var sizes = new[]
        {
            baseSize + (remainder >= 1 ? 1 : 0),
            baseSize + (remainder >= 2 ? 1 : 0),
            baseSize
        };

        var blocks = new List<IReadOnlyList<double>>(capacity: 3);
        var offset = 0;
        foreach (var size in sizes)
        {
            blocks.Add(values.Skip(offset).Take(size).ToList());
            offset += size;
        }

        return blocks;
    }
}
