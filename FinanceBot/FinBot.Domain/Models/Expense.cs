using FinBot.Domain.Models.Enums;
using FinBot.Domain.Utils;

namespace FinBot.Domain.Models;

public class Expense : IBusinessEntity<int>
{
    public int Id { get; set; }
    public ExpenseCategory Category { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }
    public Guid? GroupId { get; set; }
    public Group? Group { get; set; }
}