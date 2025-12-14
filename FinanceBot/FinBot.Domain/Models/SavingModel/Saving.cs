using FinBot.Domain.Utils;

namespace FinBot.Domain.Models.SavingModel;

public class Saving : IBusinessEntity<Guid>
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public Guid OwnerId { get; set; }
    public User? Owner { get; set; }
    
    public Guid GroupId { get; set; }
    public Group? Group { get; set; }
}