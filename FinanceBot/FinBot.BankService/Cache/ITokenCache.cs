namespace FinBot.BankService.Cache;

public interface ITokenCache
{
    Task SetAsync(Guid userId, string accessToken, TimeSpan expiry);
    Task<string?> GetAsync(Guid userId);
    Task RemoveAsync(Guid userId);
}