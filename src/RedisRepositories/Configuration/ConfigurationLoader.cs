namespace codecrafters_redis.RedisRepositories.Configuration;

internal static class ConfigurationLoader
{
   public static async Task LoadConfiguration(IRedisConfigRepository repository, string[] arguments)
   {
      var configHandlers = new Dictionary<string, Func<string[], int, Task>>()
      {
         { "--dir", async (args, index) => await HandleConfigParameter(repository, args, index, "dir") },
         { "--dbfilename", async (args, index) => await HandleConfigParameter(repository, args, index, "dbfilename") }
      };
      for (var i = 0; i < arguments.Length; i++)
      {
         if (!configHandlers.TryGetValue(arguments[i], out var handler)) continue;
         await handler(arguments, i);
         i++;
      }
   }
   private static async Task HandleConfigParameter(IRedisConfigRepository repository, string[] arguments, int index, string configKey)
   {
      if (++index >= arguments.Length)
      {
         Console.WriteLine($"Impossible to add {configKey} in config");
         return;
      }
      await repository.SetAsync(configKey, arguments[index]);
   }
}