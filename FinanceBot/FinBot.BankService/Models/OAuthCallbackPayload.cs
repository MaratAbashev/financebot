namespace FinBot.BankService.Models;

public record OAuthCallbackPayload(
    string AccessToken,
    string RefreshToken,
    string State);