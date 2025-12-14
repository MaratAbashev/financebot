using FinBot.Domain.Models.AllocationModel;
using FinBot.Domain.Utils;

namespace FinBot.Domain.Models;

public class Group : IBusinessEntity<Guid>
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public AllocationStrategy AllocationStrategy { get; set; }
    public List<Account> Accounts { get; set; }
    public Guid CreatorId { get; set; }
    public User? Creator { get; set; }
}