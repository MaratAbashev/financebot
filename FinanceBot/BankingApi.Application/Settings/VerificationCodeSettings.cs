namespace BankingApi.Application.Settings;

public class VerificationCodeSettings
{
    public int ExpiryMinutes { get; set; }
    public int Length { get; set; }
}