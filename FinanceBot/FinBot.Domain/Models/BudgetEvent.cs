using FinBot.Domain.Utils;

namespace FinBot.Domain.Models;

public class BudgetEvent :  IBusinessEntity<int>
{
    public int Id { get; set; }
    public Event Event { get; set; }
    public DateTime OccuredAt { get; set; }
    
    public Guid? PerformerId { get; set; }
    public User? Performer { get; set; }
    
    public Guid BudgetId { get; set; }
    public Budget? Budget { get; set; }
}