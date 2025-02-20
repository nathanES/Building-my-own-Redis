namespace codecrafters_redis.RedisRepositories.Configuration;

public interface IRedisConfigRepository
{
   public Task SetAsync(string key, string value);
   public Task<string?> GetAsync(string key);
}