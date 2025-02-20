namespace codecrafters_redis.RedisRepositories.KeyValue;

public interface IRedisKeyValueRepository
{
    public Task SetAsync(string key, string value, TimeSpan? expiry = null);
    public Task<string?> GetAsync(string key);
}