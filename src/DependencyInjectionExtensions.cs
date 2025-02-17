using codecrafters_redis.RedisRepositories;
using Microsoft.Extensions.DependencyInjection;

namespace codecrafters_redis;

public static class DependencyInjectionExtensions
{
   public static IServiceCollection AddDependencies(this IServiceCollection services)
   {
      var cts = new CancellationTokenSource();
      services.AddSingleton(cts);//Register only for long lived services
      
      services.AddRedisKeyValue();
      return services;
   }
}
public static class RedisKeyValueDependencyInjection
{
   public static IServiceCollection AddRedisKeyValue(this IServiceCollection services)
   {
      services.AddSingleton<IKeyValueRepository, InMemoryKeyValueRepository>();
      return services;
   }
}