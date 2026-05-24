namespace BankingApi.Domain.Entities;

public class PhoneVerificationCode
{
    public Guid Id { get; set; }
    public string PhoneNumber { get; set; }
    public string Code { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
}