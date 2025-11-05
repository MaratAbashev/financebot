using FinBot.Domain.Utils;

namespace FinBot.Domain.Models;

public class Transaction : IBusinessEntity<int>
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public DateTime OccuredAt { get; set; }
    
    public Guid AccountId { get; set; }
    public Account? Account { get; set; }
    
    public Guid SavingId { get; set; }
    public Saving? Saving { get; set; }
    
    public Guid PerformerId { get; set; }
    public User? Performer { get; set; }
}