using ClosedXML.Excel;

namespace AutoValentia;

internal sealed record ReviewExcelResult(int DayCount, int RatCount);

internal static class ReviewExcelWriter
{
    private const int MatrixColumnCount = 8;
    private const int RatPanelWidth = 12;
    private const int FirstDataRow = 3;
    private const string NumberFormat = "0.00";

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
        WriteSummary(workbook, reviews);

        foreach (var dayGroup in reviews.GroupBy(review => review.Identifier.Day).OrderBy(group => group.Key))
        {
            WriteDaySheet(workbook, dayGroup.Key, dayGroup.OrderBy(review => review.Identifier.Rat).ToList(), reviews[0]);
        }

        workbook.SaveAs(path);
        return new ReviewExcelResult(reviews.Select(review => review.Identifier.Day).Distinct().Count(), reviews.Count);
    }

    private static void WriteSummary(XLWorkbook workbook, IReadOnlyList<RatLatencyReview> reviews)
    {
        var worksheet = workbook.Worksheets.Add("Resumen");
        var title = SessionFileName.GetExperimentName(reviews[0].Source.Path);
        worksheet.Cell(1, 1).Value = title;
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 14;

        var orderedReviews = reviews
            .OrderBy(review => review.Identifier.Rat)
            .ThenBy(review => review.Identifier.Day)
            .ToList();
        for (var index = 0; index < orderedReviews.Count; index++)
        {
            var review = orderedReviews[index];
            var startColumn = 1 + index * 2;
            worksheet.Cell(2, startColumn).Value = review.Identifier.DisplayName;
            worksheet.Cell(2, startColumn).Style.Font.Bold = true;

            for (var blockIndex = 0; blockIndex < 3; blockIndex++)
            {
                WriteAverage(worksheet.Cell(FirstDataRow + blockIndex, startColumn), review.Blocks[blockIndex].SafeAverage);
                WriteAverage(worksheet.Cell(FirstDataRow + blockIndex, startColumn + 1), review.Blocks[blockIndex].RiskAverage);
            }

            worksheet.Column(startColumn).Width = 12;
            worksheet.Column(startColumn + 1).Width = 12;
        }

        worksheet.SheetView.FreezeRows(2);
    }

    private static void WriteDaySheet(
        XLWorkbook workbook,
        int day,
        IReadOnlyList<RatLatencyReview> reviews,
        RatLatencyReview firstReview)
    {
        var worksheet = workbook.Worksheets.Add($"d{day}");
        worksheet.Cell(1, 1).Value = SessionFileName.GetExperimentName(firstReview.Source.Path);
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 14;

        for (var index = 0; index < reviews.Count; index++)
        {
            WriteRatPanel(worksheet, 1 + index * RatPanelWidth, reviews[index]);
        }

        worksheet.SheetView.FreezeRows(2);
    }

    private static void WriteRatPanel(IXLWorksheet worksheet, int startColumn, RatLatencyReview review)
    {
        worksheet.Cell(2, startColumn).Value = review.Identifier.DisplayName;
        worksheet.Cell(2, startColumn).Style.Font.Bold = true;

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
            WriteAverage(worksheet.Cell(FirstDataRow + blockIndex, safeAverageColumn), review.Blocks[blockIndex].SafeAverage);
            WriteAverage(worksheet.Cell(FirstDataRow + blockIndex, riskAverageColumn), review.Blocks[blockIndex].RiskAverage);
        }

        for (var column = startColumn; column < startColumn + RatPanelWidth; column++)
        {
            worksheet.Column(column).Width = 11;
        }
    }

    private static void WriteNumber(IXLCell cell, double value)
    {
        cell.Value = value;
        cell.Style.NumberFormat.Format = NumberFormat;
    }

    private static void WriteAverage(IXLCell cell, double? value)
    {
        if (value is null)
        {
            cell.Value = "n/a";
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            return;
        }

        WriteNumber(cell, value.Value);
    }
}
