namespace codecrafters_redis.RedisRepositories.Configuration;

internal interface IRedisConfigRepository
{
   public Task SetAsync(string key, string value);
   public void Set(string key, string value);
   public Task<string?> GetAsync(string key);
   public string? Get(string key);
}