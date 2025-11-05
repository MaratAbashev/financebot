using FinBot.Domain.Utils;

namespace FinBot.Domain.Models;

public class DailyAllocation : IBusinessEntity<Guid>
{
    public Guid Id { get; set; }
    public DateTime Date { get; set; }
    public decimal Allocated { get; set; }
    public decimal Spent { get; set; }
    public decimal Leftover { get; set; }
    
    public Guid UserId { get; set; }
    public User? User { get; set; }
    
    public Guid BudgetId { get; set; }
    public Budget? Budget { get; set; }
}