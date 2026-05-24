namespace BankingApi.API.Requests;

public record OAuthSendCodeRequest(string PhoneNumber);
public record OAuthVerifyRequest(string PhoneNumber, string Code, string AuthCode);