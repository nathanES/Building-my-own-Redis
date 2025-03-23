namespace codecrafters_redis.RedisRepositories.Configuration;

internal static class ConfigurationLoader
{
   public static async Task LoadConfiguration(IRedisConfigRepository repository, string[] arguments)
   {
      var configHandlers = new Dictionary<string, Func<string[], int, Task>>()
      {
         { "--dir",  (args, index) => HandleConfigParameter(repository, args, index, ConstantsConfigurationKeys.Dir) },
         { "--dbfilename", async (args, index) => await HandleConfigParameter(repository, args, index, ConstantsConfigurationKeys.DbFileName) },
         { "--port", async(args, index) => await HandleConfigParameter(repository, args, index, ConstantsConfigurationKeys.Port) },
         { "--replicaof", async(args, index) => await HandleConfigParameter(repository, args, index, ConstantsConfigurationKeys.Replicaof) },
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
         Console.WriteLine($"[Error] - Impossible to add {configKey} in config");
         return;
      }

      Console.WriteLine($"[Debug] - Setting {configKey} to '{arguments[index]}'");
      await repository.SetAsync(configKey, arguments[index]);
   }
}