using FinBot.Domain.Utils;

namespace FinBot.Domain.Models;

public class Budget : IBusinessEntity<Guid>
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Сколько начисляется раз в период
    /// </summary>
    public decimal AllocationValue { get; set; }
    
    /// <summary>
    /// Периодичность начислений
    /// </summary>
    public AllocationPeriod Period { get; set; }
    public AllocationStrategy Strategy { get; set; }
    public AllocationData Data { get; set; }
    
    public Guid OwnerId { get; set; }
    public User? Owner { get; set; }
    
    public Guid GroupId { get; set; }
    public Group? Group { get; set; }
    
    public Guid AccountId { get; set; }
    public Account? Account { get; set; }
}