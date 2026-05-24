using FinBot.Cache;

namespace FinBot.BankService.Cache;

public class TokenCache(ICacheStorage storage) : ITokenCache
{
    private static string Key(Guid userId) => $"bank:access:{userId}";

    public async Task SetAsync(Guid userId, string accessToken, TimeSpan expiry)
        => await storage.SetAsync(Key(userId), accessToken, expiry);

    public async Task<string?> GetAsync(Guid userId)
        => await storage.GetAsync<string>(Key(userId));

    public async Task RemoveAsync(Guid userId)
        => await storage.RemoveAsync(Key(userId));
}