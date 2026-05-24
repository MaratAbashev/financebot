using FinBot.Domain.Models;
using FinBot.Domain.Models.Enums;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace FinBot.ExcelService.Reports;

public class ExcelTableBuilder : IReportBuilder
{
    public ExcelType Type => ExcelType.ExcelTable;
    public string ContentType => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public byte[] Build(IReadOnlyList<Expense> expenses, ReportRequest request)
    {
        var includeUser = request.ReportType == ReportType.ForGroup;

        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("Расходы");

        // header
        var col = 1;
        var dateCol = col++;
        var userCol = includeUser ? col++ : 0;
        var categoryCol = col++;
        var amountCol = col;

        sheet.Cells[1, dateCol].Value = "Дата";
        if (includeUser) sheet.Cells[1, userCol].Value = "Пользователь";
        sheet.Cells[1, categoryCol].Value = "Категория";
        sheet.Cells[1, amountCol].Value = "Сумма";

        using (var header = sheet.Cells[1, 1, 1, amountCol])
        {
            header.Style.Font.Bold = true;
            header.Style.Fill.PatternType = ExcelFillStyle.Solid;
            header.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
        }

        // rows
        var row = 2;
        foreach (var expense in expenses.OrderBy(e => e.Date))
        {
            sheet.Cells[row, dateCol].Value = expense.Date;
            sheet.Cells[row, dateCol].Style.Numberformat.Format = "dd.MM.yyyy";

            if (includeUser)
                sheet.Cells[row, userCol].Value = expense.User?.DisplayName ?? "—";

            sheet.Cells[row, categoryCol].Value = expense.Category.ToString();

            sheet.Cells[row, amountCol].Value = expense.Amount;
            sheet.Cells[row, amountCol].Style.Numberformat.Format = "#,##0.00 ₽";
            row++;
        }

        // --- total ---
        sheet.Cells[row, categoryCol].Value = "Итого";
        sheet.Cells[row, categoryCol].Style.Font.Bold = true;

        var sumStart = sheet.Cells[2, amountCol].Address;
        var sumEnd = sheet.Cells[row - 1, amountCol].Address;
        sheet.Cells[row, amountCol].Formula = $"SUM({sumStart}:{sumEnd})";
        sheet.Cells[row, amountCol].Style.Numberformat.Format = "#,##0.00 ₽";
        sheet.Cells[row, amountCol].Style.Font.Bold = true;

        sheet.Cells[1, 1, row, amountCol].AutoFitColumns();

        return package.GetAsByteArray();
    }
}
