namespace codecrafters_redis.RedisRepositories.Storage;

internal interface IRedisStorageRepository
{
    public Task SetAsync(string clientId,string key, string value, TimeSpan? expiry = null);
    public void Set(string clientId, string key, string value, TimeSpan? expiry = null);
    public Task<string?> GetAsync(string clientId, string key);
    public string? Get(string clientId, string key);
    public void SelectDatabase(string clientId, int dbIndex);
}