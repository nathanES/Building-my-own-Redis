using codecrafters_redis.Commands.Handlers.Config;
using codecrafters_redis.RedisRepositories.Configuration;
using codecrafters_redis.RedisRepositories.KeyValue;
using Microsoft.Extensions.DependencyInjection;

namespace codecrafters_redis.DependencyInjection;

public static class DependencyInjectionExtensions
{
   public static IServiceCollection AddDependencies(this IServiceCollection services)
   {
      var cts = new CancellationTokenSource();
      services.AddSingleton(cts);//Register only for long lived services
      
      services.RegisterKeyValueRepository();
      services.AddRedisConfigDependencies();
      
      return services;
   }
}
public static class RedisKeyValueDependencyInjection
{
   public static IServiceCollection RegisterKeyValueRepository(this IServiceCollection services)
   {
      services.AddSingleton<IRedisKeyValueRepository, InMemoryKeyValueRepository>();
      return services;
   }
}

public static class RedisConfigCommandDependencyInjection
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