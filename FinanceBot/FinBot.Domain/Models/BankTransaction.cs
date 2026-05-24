namespace FinBot.Domain.Models;

public class BankTransaction
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid ExternalId { get; set; }
    public decimal Amount { get; set; }
    public string Type { get; set; }
    public string CategoryName { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public BankTransactionStatus Status { get; set; }
}

public enum BankTransactionStatus
{
    Pending,
    Processed
}