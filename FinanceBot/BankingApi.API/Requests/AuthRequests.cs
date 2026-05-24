namespace BankingApi.API.Requests;

public record SendCodeRequest(string PhoneNumber);
public record VerifyCodeRequest(string PhoneNumber, string Code);
public record RefreshTokenRequest(string RefreshToken);