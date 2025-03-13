using codecrafters_redis.RedisRepositories.Configuration;
using Microsoft.Extensions.Hosting;

namespace codecrafters_redis.DependencyInjection;

internal class ConfigurationStartupService(IRedisConfigRepository configRepository) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Loading Redis configuration...");
        await ConfigurationLoader.LoadConfiguration(configRepository, Environment.GetCommandLineArgs().Skip(1).ToArray());
        Console.WriteLine("Configuration loaded successfully.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}