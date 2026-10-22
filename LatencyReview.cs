using MathNet.Numerics.LinearAlgebra;

namespace AutoValentia;

internal sealed record LatencyBlock(IReadOnlyList<double> Safe, IReadOnlyList<double> Risk)
{
    public double? SafeAverage => Safe.Count == 0 ? null : Math.Round(Safe.Average(), 2, MidpointRounding.AwayFromZero);

    public double? RiskAverage => Risk.Count == 0 ? null : Math.Round(Risk.Average(), 2, MidpointRounding.AwayFromZero);
}

internal sealed record RatLatencyReview(
    PreparedMatFile Source,
    SessionIdentifier Identifier,
    IReadOnlyList<double> SafeLatencies,
    IReadOnlyList<double> RiskLatencies,
    IReadOnlyList<LatencyBlock> Blocks);

internal static class LatencyReviewBuilder
{
    public static RatLatencyReview Build(PreparedMatFile file)
    {
        if (!file.IsReady || file.Matrix is null)
        {
            throw new ArgumentException($"{Path.GetFileName(file.Path)} no está preparada para el análisis.");
        }

        if (!SessionFileName.TryParse(file.Path, out var identifier) || identifier is null)
        {
            throw new ArgumentException(
                $"El nombre {Path.GetFileName(file.Path)} debe incluir el patrón dNúmero rNúmero, por ejemplo d3r4.");
        }

        var safe = new List<double>();
        var risk = new List<double>();
        AddEligibleLatencies(file.Matrix, safe, risk);

        var safeBlocks = SplitIntoThree(safe);
        var riskBlocks = SplitIntoThree(risk);
        var blocks = Enumerable.Range(0, 3)
            .Select(index => new LatencyBlock(safeBlocks[index], riskBlocks[index]))
            .ToList();

        return new RatLatencyReview(file, identifier, safe, risk, blocks);
    }

    private static void AddEligibleLatencies(
        Matrix<double> matrix,
        ICollection<double> safe,
        ICollection<double> risk)
    {
        for (var row = 0; row < matrix.RowCount; row++)
        {
            if (matrix[row, 7] <= 1d)
            {
                continue;
            }

            var stimulation = matrix[row, 2];
            var latency = matrix[row, 3];
            if (stimulation == 0d)
            {
                safe.Add(latency);
            }
            else if (stimulation == 1d)
            {
                risk.Add(latency);
            }
        }
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
