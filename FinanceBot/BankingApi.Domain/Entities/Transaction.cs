using BankingApi.Domain.Enums;

namespace BankingApi.Domain.Entities;

public class Transaction
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public Guid CategoryId { get; set; }
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }

    public Account Account { get; set; }
    public Category Category { get; set; }
}