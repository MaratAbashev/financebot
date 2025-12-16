using FinBot.Domain.Utils;

namespace FinBot.Domain.Models;

public class Saving : IBusinessEntity<Guid>
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// На что копим
    /// </summary>
    public string? Name { get; set; }
    public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public Guid GroupId { get; set; }
    public Group? Group { get; set; }
}