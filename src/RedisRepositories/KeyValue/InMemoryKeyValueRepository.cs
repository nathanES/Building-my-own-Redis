using System.Collections.Concurrent;

namespace codecrafters_redis.RedisRepositories.KeyValue;

public class InMemoryKeyValueRepository : IRedisKeyValueRepository
{
    private readonly ConcurrentDictionary<string, string> _keyValue = new();
    private readonly ConcurrentDictionary<string, DateTime> _keyExpiry = new();

    public InMemoryKeyValueRepository(CancellationTokenSource cancellationTokenSource)
    {
        Task.Run(() => CleanUpDictionaries(cancellationTokenSource.Token), cancellationTokenSource.Token);
    }

    public Task SetAsync(string key, string value, TimeSpan? expiry = null)
    {
        _keyValue[key] = value;
        _keyExpiry[key] = expiry.HasValue ? DateTime.UtcNow.Add(expiry.Value) : DateTime.MaxValue;
        return Task.CompletedTask;
    }

    public Task<string?> GetAsync(string key)
    {
        if (_keyExpiry.TryGetValue(key, out var expiry) && expiry <= DateTime.UtcNow)
        {
            _keyValue.TryRemove(key, out _);
            _keyExpiry.TryRemove(key, out _);
            return Task.FromResult<string?>(null);
        }

        return Task.FromResult(_keyValue.GetValueOrDefault(key));
    }

    private async Task CleanUpDictionaries(CancellationToken cancellationToken = default)
    {
        Console.WriteLine("Background cleanup started");
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                DateTime now = DateTime.UtcNow;
                var expiredKeys = _keyExpiry.Where(kvp => kvp.Value <= now)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in expiredKeys)
                {
                    Console.WriteLine($"Removing expired key '{key}'");
                    _keyValue.TryRemove(key, out _);
                    _keyExpiry.TryRemove(key, out _);
                }

                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Cleanup task stopped due to cancellation.");
                break;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception caught during cleanup: {e.Message}");
            }
        }
    }
}