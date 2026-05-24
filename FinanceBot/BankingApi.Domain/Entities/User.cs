namespace BankingApi.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Name { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<Account> Accounts { get; set; } = [];
}