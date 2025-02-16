namespace codecrafters_redis.RedisRepositories;

public interface IConfigRepository
{
    public Task SetAsync(string key, string value);
    public Task<string?> GetAsync(string key);
}

public class InMemoryConfigRepository : IConfigRepository
{
    private readonly Dictionary<string, string> _values = new Dictionary<string, string>();

    public async Task SetAsync(string key, string value)
    {
        if (!_values.TryAdd(key, value))
            return;
    }

    public async Task<string?> GetAsync(string key)
    {
        return _values.GetValueOrDefault(key);
    }
}