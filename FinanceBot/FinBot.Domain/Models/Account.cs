using FinBot.Domain.Models.SavingModel;
using FinBot.Domain.Utils;

namespace FinBot.Domain.Models;

public class Account : IBusinessEntity<int>
{
    public int Id { get; set; }
    public Role Role { get; set; }
    public decimal DailyAllocation { get; set; }
    public decimal MonthlyAllocation { get; set; }
    public SavingStrategy SavingStrategy { get; set; }
    
    public decimal Balance { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public Guid GroupId { get; set; }
    public Group? Group { get; set; }
}