namespace FinBot.Cache;

public interface ICacheStorage
{
    Task<T?> GetAsync<T>(string key);
    
    Task SetAsync<T>(string key, T value, TimeSpan expiration);
    
    Task RemoveAsync(string key);
}