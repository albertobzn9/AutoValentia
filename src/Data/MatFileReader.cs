using MathNet.Numerics.Data.Matlab;
using MathNet.Numerics.LinearAlgebra;

namespace AutoValentia;

internal sealed record PreparedMatFile(
    string Path,
    Matrix<double>? Matrix = null,
    int? SourceColumnCount = null,
    int RemovedRows = 0,
    string? Error = null)
{
    public bool IsReady => Matrix is not null && Error is null;

    public override string ToString()
    {
        var name = System.IO.Path.GetFileName(Path);
        if (Error is not null)
        {
            return $"✕  {name} — {Error}";
        }

        if (Matrix is null)
        {
            return $"○  {name} — pending";
        }

        return $"✓  {name} — {Matrix.RowCount} rows prepared";
    }
}

internal static class MatFileReader
{
    private const string MatrixName = "Resultados";

    public static PreparedMatFile ReadAndNormalize(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            var variables = MatlabReader.List(stream)
                .Where(variable => !variable.Name.StartsWith("__", StringComparison.Ordinal))
                .ToList();

            var matrixVariable = variables.SingleOrDefault(variable => variable.Name == MatrixName);
            if (matrixVariable is null)
            {
                return new PreparedMatFile(path, Error: "Does not contain the Resultados matrix.");
            }

            var source = MatlabReader.Unpack<double>(matrixVariable);
            return source.ColumnCount switch
            {
                8 => new PreparedMatFile(path, source, SourceColumnCount: 8),
                9 => NormalizeNineColumnMatrix(path, source),
                _ => new PreparedMatFile(
                    path,
                    Error: "The file format is not supported.")
            };
        }
        catch (Exception exception)
        {
            return new PreparedMatFile(path, Error: exception.Message);
        }
    }

    private static PreparedMatFile NormalizeNineColumnMatrix(string path, Matrix<double> source)
    {
        var acceptedRows = Enumerable.Range(0, source.RowCount)
            .Where(row => source[row, 8] != 2d)
            .ToList();
        var normalized = Matrix<double>.Build.Dense(acceptedRows.Count, 8);

        for (var normalizedRow = 0; normalizedRow < acceptedRows.Count; normalizedRow++)
        {
            var sourceRow = acceptedRows[normalizedRow];
            for (var column = 0; column < 8; column++)
            {
                normalized[normalizedRow, column] = source[sourceRow, column];
            }

            normalized[normalizedRow, 0] = normalizedRow + 1;
        }

        return new PreparedMatFile(
            path,
            normalized,
            SourceColumnCount: 9,
            RemovedRows: source.RowCount - acceptedRows.Count);
    }
}
