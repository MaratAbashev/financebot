using BankingApi.Domain.Enums;

namespace BankingApi.Domain.Entities;

public class Category
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public CategoryType Type { get; set; }

    public ICollection<Transaction> Transactions { get; set; } = [];
}