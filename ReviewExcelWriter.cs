using ClosedXML.Excel;

namespace AutoValentia;

internal sealed record ReviewExcelResult(int DayCount, int RatCount);

internal static class ReviewExcelWriter
{
    private const int MatrixColumnCount = 8;
    private const int RatPanelWidth = 12;
    private const int FirstDataRow = 3;
    public static ReviewExcelResult Write(string path, IReadOnlyCollection<PreparedMatFile> files)
    {
        var reviews = files
            .Select(LatencyReviewBuilder.Build)
            .OrderBy(review => review.Identifier.Day)
            .ThenBy(review => review.Identifier.Rat)
            .ToList();

        if (reviews.Count == 0)
        {
            throw new InvalidOperationException("No hay matrices listas para exportar.");
        }

        var duplicate = reviews.GroupBy(review => review.Identifier).FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidOperationException($"Hay más de un archivo identificado como {duplicate.Key.DisplayName}.");
        }

        using var workbook = new XLWorkbook();
        workbook.Style.Font.FontName = "Aptos Narrow";
        workbook.Style.Font.FontSize = 11;
        WriteSummary(workbook, reviews);

        foreach (var dayGroup in reviews.GroupBy(review => review.Identifier.Day).OrderBy(group => group.Key))
        {
            WriteDaySheet(workbook, dayGroup.OrderBy(review => review.Identifier.Rat).ToList(), reviews[0]);
        }

        workbook.SaveAs(path);
        return new ReviewExcelResult(reviews.Select(review => review.Identifier.Day).Distinct().Count(), reviews.Count);
    }

    private static void WriteSummary(XLWorkbook workbook, IReadOnlyList<RatLatencyReview> reviews)
    {
        var worksheet = workbook.Worksheets.Add("Resumen");
        var title = SessionFileName.GetExperimentName(reviews[0].Source.Path);
        worksheet.Cell(1, 1).Value = title;
        var days = reviews.Select(review => review.Identifier.Day).Distinct().OrderBy(day => day).ToList();
        var rats = reviews.Select(review => review.Identifier.Rat).Distinct().OrderBy(rat => rat).ToList();
        for (var ratIndex = 0; ratIndex < rats.Count; ratIndex++)
        {
            var startRow = 2 + ratIndex * 4;
            for (var dayIndex = 0; dayIndex < days.Count; dayIndex++)
            {
                var review = reviews.SingleOrDefault(candidate =>
                    candidate.Identifier.Day == days[dayIndex] && candidate.Identifier.Rat == rats[ratIndex]);
                if (review is null)
                {
                    continue;
                }

                var startColumn = 1 + dayIndex * 2;
                worksheet.Cell(startRow, startColumn).Value = review.Identifier.DisplayName;
                worksheet.Cell(startRow, startColumn).Style.Font.Bold = true;

                for (var blockIndex = 0; blockIndex < 3; blockIndex++)
                {
                    WriteAverage(worksheet.Cell(startRow + 1 + blockIndex, startColumn), review.Blocks[blockIndex].SafeAverage, bold: false);
                    WriteAverage(worksheet.Cell(startRow + 1 + blockIndex, startColumn + 1), review.Blocks[blockIndex].RiskAverage, bold: false);
                }
            }
        }
    }

    private static void WriteDaySheet(
        XLWorkbook workbook,
        IReadOnlyList<RatLatencyReview> reviews,
        RatLatencyReview firstReview)
    {
        var worksheet = workbook.Worksheets.Add(reviews[0].Identifier.DaySheetName);
        worksheet.Cell(1, 1).Value = SessionFileName.GetExperimentName(firstReview.Source.Path);

        for (var index = 0; index < reviews.Count; index++)
        {
            WriteRatPanel(worksheet, 1 + index * RatPanelWidth, reviews[index]);
        }

    }

    private static void WriteRatPanel(IXLWorksheet worksheet, int startColumn, RatLatencyReview review)
    {
        worksheet.Cell(2, startColumn).Value = review.Identifier.DisplayName;

        var matrix = review.Source.Matrix!;
        for (var row = 0; row < matrix.RowCount; row++)
        {
            for (var column = 0; column < MatrixColumnCount; column++)
            {
                WriteNumber(worksheet.Cell(FirstDataRow + row, startColumn + column), matrix[row, column]);
            }
        }

        var safeColumn = startColumn + MatrixColumnCount;
        var riskColumn = safeColumn + 1;
        for (var row = 0; row < Math.Max(review.SafeLatencies.Count, review.RiskLatencies.Count); row++)
        {
            if (row < review.SafeLatencies.Count)
            {
                WriteNumber(worksheet.Cell(FirstDataRow + row, safeColumn), review.SafeLatencies[row]);
            }

            if (row < review.RiskLatencies.Count)
            {
                WriteNumber(worksheet.Cell(FirstDataRow + row, riskColumn), review.RiskLatencies[row]);
            }
        }

        var safeAverageColumn = riskColumn + 1;
        var riskAverageColumn = riskColumn + 2;
        for (var blockIndex = 0; blockIndex < 3; blockIndex++)
        {
            WriteAverage(worksheet.Cell(FirstDataRow + blockIndex, safeAverageColumn), review.Blocks[blockIndex].SafeAverage, bold: true);
            WriteAverage(worksheet.Cell(FirstDataRow + blockIndex, riskAverageColumn), review.Blocks[blockIndex].RiskAverage, bold: true);
        }
    }

    private static void WriteNumber(IXLCell cell, double value)
    {
        cell.Value = value;
    }

    private static void WriteAverage(IXLCell cell, double? value, bool bold)
    {
        if (value is null)
        {
            cell.Value = "n/a";
            cell.Style.Font.Bold = bold;
            return;
        }

        WriteNumber(cell, value.Value);
        cell.Style.Font.Bold = bold;
    }
}
