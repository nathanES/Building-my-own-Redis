using System.Reflection;
using codecrafters_redis.RedisCommands.Handlers;

namespace codecrafters_redis.RedisCommands;

public class RedisCommandsRegistry
{
    private readonly Dictionary<RedisCommand, IRedisCommandHandler> _handlers = new();
    public RedisCommandsRegistry(IServiceProvider serviceProvider)
    {
        var handlerTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => typeof(IRedisCommandHandler).IsAssignableFrom(t) && t is { IsInterface: false, IsAbstract: false });
        foreach (var handlerType in handlerTypes)
        {
            var constructor = handlerType.GetConstructors().FirstOrDefault();
            if(constructor is null)
                continue;
            var parameters = constructor.GetParameters()
                .Select(p => serviceProvider.GetService(p.ParameterType))
                .ToArray();
            if (Activator.CreateInstance(handlerType, parameters) is IRedisCommandHandler handler)
            {
                _handlers[handler.Command] = handler;
            }
        }    
    }
    public IRedisCommandHandler GetHandler(RedisCommand command)
        => _handlers.TryGetValue(command, out var handler)? handler : new UnknownCommandHandler();
}