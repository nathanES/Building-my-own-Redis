using System.Collections.Concurrent;
using System.Text;
using codecrafters_redis.RedisRepositories.Configuration;

namespace codecrafters_redis.RedisRepositories.Storage;

internal class InMemoryStorageRepository : IRedisStorageRepository
{
    private readonly IRedisConfigRepository _configRepository;
    private readonly ConcurrentDictionary<int, ConcurrentDictionary<string, string>> _databases = new();
    private readonly ConcurrentDictionary<int, ConcurrentDictionary<string, DateTime>> _databaseExpiries = new();

    private readonly ConcurrentDictionary<string, int> _clientDbSelections = new();

    public InMemoryStorageRepository(CancellationTokenSource cancellationTokenSource,
        IRedisConfigRepository configRepository)
    {
        _configRepository = configRepository;
        Task.Run(() => CleanUpDictionaries(cancellationTokenSource.Token), cancellationTokenSource.Token);
    }

    public async Task LoadConfigurationAsync()
    {
        var dir = await _configRepository.GetAsync("dir");
        var fileName = await _configRepository.GetAsync("dbfilename");
        await LoadDictionariesAsync(Path.Combine(dir!, fileName!));
    }

    public void LoadConfiguration()
    {
        LoadConfigurationAsync().Wait();
    }

    public Task SetAsync(string clientId, string key, string value, TimeSpan? expiry = null)
    {
        Set(clientId, key, value, expiry);
        return Task.CompletedTask;
    }

    public void Set(string clientId, string key, string value, TimeSpan? expiry = null)
    {
        int db = _clientDbSelections.GetValueOrDefault(clientId, 0);
        var keyValueStore = _databases.GetOrAdd(db, _ => new ConcurrentDictionary<string, string>());
        var keyExpiryStore = _databaseExpiries.GetOrAdd(db, _ => new ConcurrentDictionary<string, DateTime>());

        keyValueStore[key] = value;
        keyExpiryStore[key] = expiry.HasValue ? DateTime.UtcNow.Add(expiry.Value) : DateTime.MaxValue;
    }

    public Task<string?> GetAsync(string clientId, string key)
    {
        return Task.FromResult(Get(clientId, key));
    }

    public string? Get(string clientId, string key)
    {
        int db = _clientDbSelections.GetValueOrDefault(clientId, 0);
        if (!_databaseExpiries.TryGetValue(db, out var dbExpiries)
            || !dbExpiries.TryGetValue(key, out var expiry))
        {
            return _databases.TryGetValue(db, out var dbKeyValue) ? dbKeyValue.GetValueOrDefault(key) : null;
        }

        if (expiry <= DateTime.UtcNow)
        {
            _databases[db].TryRemove(key, out _);
            dbExpiries.TryRemove(key, out _);
        }

        return null;
    }

    public void SelectDatabase(string clientId, int dbIndex)
    {
        if (dbIndex < 0)
            throw new ArgumentOutOfRangeException($"Invalid db index: {dbIndex}");
        _clientDbSelections.AddOrUpdate(clientId, dbIndex, (_, _) => dbIndex);

        Console.WriteLine($"Client: {clientId} switched to DB: {dbIndex}");
    }

    private async Task CleanUpDictionaries(CancellationToken cancellationToken = default)
    {
        Console.WriteLine("Background cleanup started");
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                DateTime now = DateTime.UtcNow;
                foreach (var dbExpiryKvp in _databaseExpiries)
                {
                    if (!_databases.TryGetValue(dbExpiryKvp.Key, out var dbStore)) 
                        continue;
                    
                    var expiredKeys = dbExpiryKvp.Value.Where(kvp => kvp.Value <= now)
                        .Select(kvp => kvp.Key)
                        .ToList();

                    foreach (var key in expiredKeys)
                    {
                        dbStore.TryRemove(key, out _);
                        _databaseExpiries[dbExpiryKvp.Key].TryRemove(key, out _);
                    }
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

    private async Task LoadDictionariesAsync(string rdbFilePath)
    {
        if (!File.Exists(rdbFilePath))
            return;
        using FileStream fs = File.Open(rdbFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var rdbFile = await RdbFileConverter.ParseToRdbFileAsync(fs);
        if (rdbFile == null)
            return;
        //Validate checksum
 
        foreach (var dbKvp in rdbFile.Databases)
        {
            var keyValueStore =
                _databases.GetOrAdd(dbKvp.Value.DatabaseIndex, _ => new ConcurrentDictionary<string, string>());

            foreach (var keyValueKvp in dbKvp.Value.KeyValues)
            {
                keyValueStore.TryAdd(keyValueKvp.Key, Encoding.UTF8.GetString(keyValueKvp.Value.Value));
            }
        }
    }
}