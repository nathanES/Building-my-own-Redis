using codecrafters_redis.Commands.Handlers.Config;
using codecrafters_redis.RedisRepositories.Configuration;
using codecrafters_redis.RedisRepositories.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace codecrafters_redis.DependencyInjection;

internal static class DependencyInjectionExtensions
{
   public static IServiceCollection AddDependencies(this IServiceCollection services)
   {
      var cts = new CancellationTokenSource();
      services.AddSingleton(cts);//Register only for long lived services
      
      services.RegisterStorageRepository();
      services.AddRedisConfigDependencies();
      
      services.AddHostedService<ConfigurationStartupService>();
      
      return services;
   }
}
internal static class RedisStorageDependencyInjection
{
   public static IServiceCollection RegisterStorageRepository(this IServiceCollection services)
   {
      services.AddSingleton<IRedisStorageRepository, InMemoryStorageRepository>();
      return services;
   }
}

internal static class RedisConfigCommandDependencyInjection
{
   public static IServiceCollection AddRedisConfigDependencies(this IServiceCollection services)
   {
      services.RegisterRedisConfigRepository();
      services.RegisterRedisConfigCommandHandler();
      return services;
   }
   private static IServiceCollection RegisterRedisConfigRepository(this IServiceCollection services)
   {
      services.AddSingleton<IRedisConfigRepository, InMemoryConfigRepository>();
      return services;
   }

   private static IServiceCollection RegisterRedisConfigCommandHandler(this IServiceCollection services)
   {
      services.AddSingleton<ConfigGetCommandHandler>();
      return services; 
   }
}