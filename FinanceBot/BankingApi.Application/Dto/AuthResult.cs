namespace BankingApi.Application.Dto;

public class AuthResult
{
    public bool Succeeded { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public string? Error { get; set; }

    public static AuthResult Success(string accessToken, string refreshToken) => new()
    {
        Succeeded = true,
        AccessToken = accessToken,
        RefreshToken = refreshToken
    };

    public static AuthResult Failure(string error) => new()
    {
        Succeeded = false,
        Error = error
    };
}