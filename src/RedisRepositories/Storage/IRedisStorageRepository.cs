namespace codecrafters_redis.RedisRepositories.Storage;

internal interface IRedisStorageRepository
{
    public Task LoadConfigurationAsync();
    public void LoadConfiguration();
    public Task SetAsync(string clientId,string key, string value, TimeSpan? expiry = null);
    public void Set(string clientId, string key, string value, TimeSpan? expiry = null);
    public Task<IEnumerable<(string Key, string Value)>> GetAsync(string clientId, Func<string, bool> keyPattern);
    public IEnumerable<(string Key, string Value)> Get(string clientId, Func<string,bool> keyPattern);
    public void SelectDatabase(string clientId, int dbIndex);
}