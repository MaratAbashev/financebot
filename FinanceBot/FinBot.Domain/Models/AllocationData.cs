namespace FinBot.Domain.Models;

public class AllocationData
{
    public int UserId { get; set; }
    public AllocationType Type { get; set; }
    public decimal Value { get; set; }
}