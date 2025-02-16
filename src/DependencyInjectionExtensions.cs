using codecrafters_redis.RedisRepositories;
using Microsoft.Extensions.DependencyInjection;

namespace codecrafters_redis;

public static class DependencyInjectionExtensions
{
   public static IServiceCollection AddDependencies(this IServiceCollection services)
   {
      services.AddRedisConfiguration();
      return services;
   }
}
//TODO trouver un meilleur nom
public static class RedisConfigurationDependencyInjection
{
   public static IServiceCollection AddRedisConfiguration(this IServiceCollection services)
   {
      services.AddSingleton<IConfigRepository>(new InMemoryConfigRepository());
      return services;
   }
}