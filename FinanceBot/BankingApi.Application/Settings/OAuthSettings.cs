namespace BankingApi.Application.Settings;

public class OAuthSettings
{
    public string BaseUrl { get; set; }
    public int SessionExpiryMinutes { get; set; }
}