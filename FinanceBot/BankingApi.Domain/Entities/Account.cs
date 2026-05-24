using BankingApi.Domain.Enums;

namespace BankingApi.Domain.Entities;

public class Account
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = default!;
    public decimal Balance { get; set; }
    public Currency Currency { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = default!;
    public ICollection<Transaction> Transactions { get; set; } = [];
}