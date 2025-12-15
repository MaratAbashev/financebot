using FinBot.Domain.Models.Enums;
using FinBot.Domain.Models.SavingModel;
using FinBot.Domain.Utils;

namespace FinBot.Domain.Models;

public class Group : IBusinessEntity<Guid>
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public decimal GroupBalance { get; set; }
    public decimal MonthlyReplenishment { get; set; }
    public SavingStrategy SavingStrategy { get; set; }
    public DebtStrategy DebtStrategy { get; set; }
    public List<Account> Accounts { get; set; }
    public Guid CreatorId { get; set; }
    public User? Creator { get; set; }
    public Saving? Saving { get; set; }
}