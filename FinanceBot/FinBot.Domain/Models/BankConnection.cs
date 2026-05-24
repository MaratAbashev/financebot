namespace FinBot.Domain.Models;

public class BankConnection
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string BankingApiBaseUrl { get; set; }
    public string RefreshToken { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}