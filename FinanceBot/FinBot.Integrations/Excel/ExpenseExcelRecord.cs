using FinBot.Domain.Models;

namespace FinBot.Integrations.Excel;

public class ExpenseExcelRecord
{
    [ExcelColumn("ID")]
    public int Id { get; set; }
    
    [ExcelColumn("Категория")]
    public required string ExpenseCategory { get; set; }
    
    [ExcelColumn("Величина")]
    public decimal Amount { get; set; }
    
    [ExcelColumn("Дата")]
    public DateTime Date { get; set; }
    
    [ExcelColumn("Пользователь")]
    public string? UserName { get; set; }

    public static ExpenseExcelRecord GetExpenseRecord(Expense expense)
    {
        var record = new ExpenseExcelRecord
        {
            Id = expense.Id,
            ExpenseCategory = expense.Category.ToString(),
            Amount = expense.Amount,
            Date = expense.Date,
            UserName = expense.Account?.User?.DisplayName ?? string.Empty
        };
        
        return record;
    }

    public static List<ExpenseExcelRecord> GetExpenseRecords(List<Expense> expenses)
    {
        return expenses.Select((x, i) =>
        {
            var expense = GetExpenseRecord(x);
            expense.Id = i;
            return expense;
        }).ToList();
    }
}
