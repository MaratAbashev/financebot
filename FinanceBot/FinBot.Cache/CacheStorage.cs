using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using StackExchange.Redis;

namespace FinBot.Cache;

public class CacheStorage : ICacheStorage
{
    private static readonly JsonSerializerOptions Options = new()
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    };

    private readonly ConnectionMultiplexer _multiplexer;
    private readonly IDatabase _database;

    public CacheStorage(string connectionString)
    {
        var options = ConfigurationOptions.Parse(connectionString);

        options.AbortOnConnectFail = false;
        options.ConnectRetry = 3;
        options.ReconnectRetryPolicy = new ExponentialRetry(5000);
        options.KeepAlive = 10;

        _multiplexer = ConnectionMultiplexer.Connect(options);
        _database = _multiplexer.GetDatabase();
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var value = await ExecuteWithFailover(async db =>
            await db.StringGetAsync(key));

        if (value.IsNullOrEmpty)
            return default;

        return JsonSerializer.Deserialize<T>(value.ToString(), Options);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan expiration)
    {
        var serialized = JsonSerializer.Serialize(value, Options);

        await ExecuteWithFailover(async db =>
        {
            await db.StringSetAsync(key, serialized, expiration);
            return true;
        });
    }

    public async Task RemoveAsync(string key)
    {
        await ExecuteWithFailover(async db =>
        {
            await db.KeyDeleteAsync(key);
            return true;
        });
    }

    private async Task<T> ExecuteWithFailover<T>(Func<IDatabase, Task<T>> action)
    {
        try
        {
            return await action(_database);
        }
        catch (RedisConnectionException)
        {
            await _multiplexer.ConfigureAsync();
            return await action(_database);
        }
        catch (RedisTimeoutException)
        {
            await _multiplexer.ConfigureAsync();
            return await action(_database);
        }
    }
}