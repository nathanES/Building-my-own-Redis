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
        => SetAsync(key, value).Wait();

    public Task<string?> GetAsync(string key)
        => Task.FromResult(_redisConfigurations.GetValueOrDefault(key));

    public string? Get(string key)
        => GetAsync(key).Result;

    public Task<IEnumerable<(string Key, string Value)>> GetByKeyPatternAsync(Func<string, bool> pattern)
        => Task.FromResult(GetByKeyPattern(pattern));
    public IEnumerable<(string Key, string Value)> GetByKeyPattern(Func<string, bool> pattern)
    {
        foreach (var pair in _redisConfigurations.Where(kvp => pattern(kvp.Key)))
            yield return (pair.Key, pair.Value);
    }


}