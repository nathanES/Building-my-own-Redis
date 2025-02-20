using System.Collections.Concurrent;

namespace codecrafters_redis.RedisRepositories.Configuration;

public class InMemoryConfigRepository : IRedisConfigRepository
{
    private readonly ConcurrentDictionary<string, string> _redisConfigurations= new ();
   
    public Task SetAsync(string key, string value)
    {
        _redisConfigurations[key] = value;
        return Task.CompletedTask;
    }

    public Task<string?> GetAsync(string key)
    {
        return Task.FromResult(_redisConfigurations.GetValueOrDefault(key));
    }
}