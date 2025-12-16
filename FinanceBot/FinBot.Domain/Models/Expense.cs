using FinBot.Domain.Models.Enums;
using FinBot.Domain.Utils;

namespace FinBot.Domain.Models;

public class Expense : IBusinessEntity<int>
{
    public int Id { get; set; }
    public ExpenseCategory Category { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    
    public int AccountId { get; set; }
    public Account? Account { get; set; }
}