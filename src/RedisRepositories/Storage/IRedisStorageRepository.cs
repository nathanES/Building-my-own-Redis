namespace codecrafters_redis.RedisRepositories.Storage;

internal interface IRedisStorageRepository
{
    public Task LoadConfigurationAsync();
    public void LoadConfiguration();
    public Task SetAsync(string clientId,string key, string value, TimeSpan? expiry = null);
    public void Set(string clientId, string key, string value, TimeSpan? expiry = null);
    public Task<IEnumerable<(string Key, string Value)>> GetByKeyPatternAsync(string clientId, Func<string, bool> pattern);
    public IEnumerable<(string Key, string Value)> GetByKeyPattern(string clientId, Func<string,bool> pattern);
    public void SelectDatabase(string clientId, int dbIndex);
}