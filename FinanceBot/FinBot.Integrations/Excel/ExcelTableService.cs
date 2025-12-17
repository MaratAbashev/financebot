using System.Globalization;
using System.Reflection;
using FinBot.Bll.Interfaces;
using FinBot.Bll.Interfaces.Integration;
using FinBot.Dal.DbContexts;
using FinBot.Domain.Models;
using FinBot.Domain.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;

namespace FinBot.Integrations.Excel;

public class ExcelTableService : IExcelTableService
{
    private readonly IGenericRepository<Expense, int, PDbContext> _repository;    
    private readonly ILogger<ExcelTableService> _logger;

    public ExcelTableService(
        IGenericRepository<Expense, int, PDbContext> repository,
        ILogger<ExcelTableService> logger)
    {
        _logger = logger;
        _repository = repository;

        ExcelPackage.License.SetNonCommercialPersonal("My PC");
    }

    public async Task<Result<byte[]>> ExportToExcelForGroupAsync(Guid groupId, int months)
    {
        try
        {
            var expenses = await _repository.GetAll()
                .Include(e => e.Account)
                .Where(e => e.Account!.GroupId == groupId)
                .OrderByDescending(e => e.Date)
                .ToListAsync();
            
            return Result<byte[]>.Success(await ExportToExcelAsync(expenses, months));
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to get expenses for group {groupId} with message : {errorMessage}\nErrorStack{errorStack}", groupId, ex.Message, ex.StackTrace);
            return Result<byte[]>.Failure(ex.Message);
        }
    }

    public async Task<Result<byte[]>> ExportToExcelForUserInGroupAsync(Guid userId, Guid groupId, int months)
    {
        try
        {
            var expenses = await _repository.GetAll()
                .Include(e => e.Account)
                .Where(e => e.Account!.GroupId == groupId && e.Account!.UserId == userId)
                .OrderByDescending(e => e.Date)
                .ToListAsync();
            
            return Result<byte[]>.Success(await ExportToExcelAsync(expenses, months));
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to get expenses for user {userId} in group {groupId} with message : {errorMessage}\nErrorStack{errorStack}", userId, groupId, ex.Message, ex.StackTrace);
            return Result<byte[]>.Failure(ex.Message);
        }
    }
    
    private async Task<byte[]> ExportToExcelAsync(List<Expense> expenses, int months)
    {
        var expenseRecords = ExpenseExcelRecord.GetExpenseRecords(expenses);
    
        using var package = new ExcelPackage();
    
        var today = DateTime.Today;
        var culture = CultureInfo.GetCultureInfo("ru-RU");

        for (int i = 0; i < months; i++)
        {
            var targetDate = today.AddMonths(-i);
        
            var expensesForMonth = expenseRecords
                .Where(r => r.Date.Year == targetDate.Year && r.Date.Month == targetDate.Month)
                .ToList();

            var monthName = targetDate.ToString("MMMM yyyy", culture);
            var sheetName = culture.TextInfo.ToTitleCase(monthName);

            AddSheetFromData(package, sheetName, expensesForMonth);
        }

        return await package.GetAsByteArrayAsync();
    }

    private static void AddSheetFromData<T>(ExcelPackage package, string sheetName, IEnumerable<T> data)
    {
        var worksheet = package.Workbook.Worksheets.Add(sheetName);
        
        var properties = typeof(T).GetProperties()
            .Where(p => p.IsDefined(typeof(ExcelColumnAttribute), false))
            .ToArray();

        for (var i = 0; i < properties.Length; i++)
        {
            var header = properties[i].GetCustomAttribute<ExcelColumnAttribute>()?.Name ?? properties[i].Name;
            worksheet.Cells[1, i + 1].Value = header;
        }

        var row = 2;
        foreach (var item in data)
        {
            for (var col = 0; col < properties.Length; col++)
            {
                var value = properties[col].GetValue(item);
                var cell = worksheet.Cells[row, col + 1];

                if (value is DateTime dt)
                {
                    cell.Value = dt;
                    cell.Style.Numberformat.Format = "dd.MM.yyyy";
                }
                else
                {
                    cell.Value = value;
                }
            }
            row++;
        }
        
        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
    }
}