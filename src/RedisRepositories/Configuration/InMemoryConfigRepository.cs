using System.Collections.Concurrent;

namespace codecrafters_redis.RedisRepositories.Configuration;

internal class InMemoryConfigRepository : IRedisConfigRepository
{
    private readonly ConcurrentDictionary<string, string> _redisConfigurations = new();

    public Task SetAsync(string key, string value)
    {
        _redisConfigurations[key] = value;
        return Task.CompletedTask;
    }

    public void Set(string key, string value)
    {
        _redisConfigurations[key] = value;
    }

    public Task<string?> GetAsync(string key)
    {
        return Task.FromResult(_redisConfigurations.GetValueOrDefault(key));
    }

    public string? Get(string key)
    {
        return _redisConfigurations.GetValueOrDefault(key);
    }
}