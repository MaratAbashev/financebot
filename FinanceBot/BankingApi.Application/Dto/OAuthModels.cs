namespace BankingApi.Application.Dto;

public record InitiateOAuthResult(string AuthUrl);

public record OAuthCallbackPayload(
    string AccessToken,
    string RefreshToken,
    string State);