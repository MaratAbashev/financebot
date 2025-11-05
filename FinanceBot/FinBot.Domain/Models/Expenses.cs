using FinBot.Domain.Utils;

namespace FinBot.Domain.Models;

public class Expenses : IBusinessEntity<int>
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public Category Category { get; set; }
    public DateTime OccuredAt { get; set; }
    
    public Guid UserId { get; set; }
    public User? User { get; set; }
    
    public Guid GroupId { get; set; }
    public Group? Group { get; set; }
    
    public Guid AccountId { get; set; }
    public Account? Account { get; set; }
    
    public Guid BudgetId { get; set; }
    public Budget? Budget { get; set; }
    
    public Guid AllocationId { get; set; }
    public DailyAllocation? DailyAllocation { get; set; }
}