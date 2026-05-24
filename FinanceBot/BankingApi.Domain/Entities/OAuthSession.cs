using BankingApi.Domain.Enums;

namespace BankingApi.Domain.Entities;

public class OAuthSession
{
    public Guid Id { get; set; }
    public string AuthCode { get; set; }
    public string RedirectUri { get; set; }
    public string State { get; set; }
    public Guid? UserId { get; set; }
    public OAuthSessionStatus Status { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public User? User { get; set; }
}