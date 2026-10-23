using ClosedXML.Excel;

namespace AutoValentia;

internal sealed record ReviewExcelResult(int DayCount, int RatCount, CrossingType CrossingType);

internal static class ReviewExcelWriter
{
    private const int MatrixColumnCount = 8;
    private const int RatPanelWidth = 12;
    private const int FirstDataRow = 3;
    public static ReviewExcelResult Write(string path, IReadOnlyCollection<PreparedMatFile> files)
    {
        var reviewSet = LatencyReviewSetBuilder.Build(files);
        var reviews = reviewSet.Reviews;
        var crossingType = reviewSet.CrossingType;

        using var workbook = new XLWorkbook();
        workbook.Style.Font.FontName = "Aptos Narrow";
        workbook.Style.Font.FontSize = 11;
        WriteSummary(workbook, reviews, crossingType);

        foreach (var dayGroup in reviews.GroupBy(review => review.Identifier.Day).OrderBy(group => group.Key))
        {
            WriteDaySheet(workbook, dayGroup.OrderBy(review => review.Identifier.Rat).ToList(), reviews[0], crossingType);
        }

        workbook.SaveAs(path);
        return new ReviewExcelResult(
            reviews.Select(review => review.Identifier.Day).Distinct().Count(),
            reviews.Count,
            crossingType);
    }

    private static void WriteSummary(
        XLWorkbook workbook,
        IReadOnlyList<RatLatencyReview> reviews,
        CrossingType crossingType)
    {
        var worksheet = workbook.Worksheets.Add("Resumen");
        var title = SessionFileName.GetExperimentName(reviews[0].Source.Path);
        worksheet.Cell(1, 1).Value = title;
        worksheet.Cell(1, 3).Value = crossingType.DisplayName();
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

        WriteSummaryStatistics(worksheet, days, rats);
    }

    private static void WriteSummaryStatistics(
        IXLWorksheet worksheet,
        IReadOnlyList<decimal> days,
        IReadOnlyList<int> rats)
    {
        var averageLabelRow = 2 + rats.Count * 4;
        worksheet.Cell(averageLabelRow, 1).Value = "average";

        for (var blockIndex = 0; blockIndex < 3; blockIndex++)
        {
            var averageRow = averageLabelRow + 1 + blockIndex;
            var semRow = averageLabelRow + 5 + blockIndex;
            for (var dayIndex = 0; dayIndex < days.Count; dayIndex++)
            {
                for (var conditionOffset = 0; conditionOffset < 2; conditionOffset++)
                {
                    var column = 1 + dayIndex * 2 + conditionOffset;
                    var blockCells = rats
                        .Select((_, ratIndex) => worksheet.Cell(3 + ratIndex * 4 + blockIndex, column))
                        .ToList();
                    var references = string.Join(",", blockCells.Select(cell => cell.Address.ToString()));
                    WriteSummaryAverage(worksheet.Cell(averageRow, column), references);
                    WriteSummarySem(worksheet.Cell(semRow, column), references);
                }
            }
        }

        worksheet.Cell(averageLabelRow + 4, 1).Value = "sem";
    }

    private static void WriteDaySheet(
        XLWorkbook workbook,
        IReadOnlyList<RatLatencyReview> reviews,
        RatLatencyReview firstReview,
        CrossingType crossingType)
    {
        var worksheet = workbook.Worksheets.Add(reviews[0].Identifier.DaySheetName);
        worksheet.Cell(1, 1).Value = SessionFileName.GetExperimentName(firstReview.Source.Path);
        worksheet.Cell(1, 3).Value = crossingType.DisplayName();

        for (var index = 0; index < reviews.Count; index++)
        {
            WriteRatPanel(worksheet, 1 + index * RatPanelWidth, reviews[index]);
        }

    }

    private static void WriteRatPanel(IXLWorksheet worksheet, int startColumn, RatLatencyReview review)
    {
        worksheet.Cell(2, startColumn).Value = review.Identifier.DisplayName;

        var matrix = review.Source.Matrix!;
        for (var row = 0; row < review.MatrixRowsForExport; row++)
        {
            for (var column = 0; column < MatrixColumnCount; column++)
            {
                WriteMatrixNumber(worksheet.Cell(FirstDataRow + row, startColumn + column), matrix[row, column]);
            }
        }

        var safeColumn = startColumn + MatrixColumnCount;
        var riskColumn = safeColumn + 1;
        WriteBlockDetails(worksheet, safeColumn, review.Blocks, block => block.Safe, block => block.SafeAverage);
        WriteBlockDetails(worksheet, riskColumn, review.Blocks, block => block.Risk, block => block.RiskAverage);

        var safeAverageColumn = riskColumn + 1;
        var riskAverageColumn = riskColumn + 2;
        for (var blockIndex = 0; blockIndex < 3; blockIndex++)
        {
            WriteAverage(worksheet.Cell(FirstDataRow + blockIndex, safeAverageColumn), review.Blocks[blockIndex].SafeAverage, bold: true);
            WriteAverage(worksheet.Cell(FirstDataRow + blockIndex, riskAverageColumn), review.Blocks[blockIndex].RiskAverage, bold: true);
        }
    }

    private static void WriteBlockDetails(
        IXLWorksheet worksheet,
        int column,
        IReadOnlyList<LatencyBlock> blocks,
        Func<LatencyBlock, IReadOnlyList<double>> valuesSelector,
        Func<LatencyBlock, double?> averageSelector)
    {
        var row = FirstDataRow;
        for (var blockIndex = 0; blockIndex < blocks.Count; blockIndex++)
        {
            foreach (var value in valuesSelector(blocks[blockIndex]))
            {
                WriteNumber(worksheet.Cell(row, column), value);
                row++;
            }

            WriteAverage(worksheet.Cell(row, column), averageSelector(blocks[blockIndex]), bold: true);
            row++;

            // The empty row is the visual separation between adjacent thirds.
            if (blockIndex < blocks.Count - 1)
            {
                row++;
            }
        }
    }

    private static void WriteNumber(IXLCell cell, double value)
    {
        cell.Value = value;
        cell.Style.NumberFormat.Format = "0.00";
    }

    private static void WriteMatrixNumber(IXLCell cell, double value)
    {
        cell.Value = value;
        cell.Style.NumberFormat.Format = value == Math.Truncate(value) ? "0" : "0.00";
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

    private static void WriteSummaryAverage(IXLCell cell, string references)
    {
        cell.FormulaA1 = $"IF(COUNT({references})=0,\"n/a\",AVERAGE({references}))";
        cell.Style.NumberFormat.Format = "0.00";
        cell.Style.Font.Bold = true;
    }

    private static void WriteSummarySem(IXLCell cell, string references)
    {
        cell.FormulaA1 = $"IF(COUNT({references})=0,\"n/a\",IF(COUNT({references})=1,0,STDEV.S({references})/SQRT(COUNT({references}))))";
        cell.Style.NumberFormat.Format = "0.00";
        cell.Style.Font.Bold = true;
    }
}
