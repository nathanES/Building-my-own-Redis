using codecrafters_redis.RedisRepositories;
using codecrafters_redis.RespRequestResponse;

namespace codecrafters_redis.RedisCommands.Handlers;

public class SetCommandHandler(IConfigRepository configRepository) : IRedisCommandHandler
{
    private readonly IConfigRepository _configRepository = configRepository;
    public RedisCommand Command => RedisCommand.Set;

    public RespResponse Handle(RespRequest request)
    {
        if(request.Arguments.Count < 2)
            RespResponse.FromError(request.Arguments.Count + " arguments must be at least 2");
        _configRepository.SetAsync(request.Arguments[0], request.Arguments[1]);
        return RespResponse.FromSimpleString("OK");
    }
}

public class GetCommandHandler(IConfigRepository configRepository) : IRedisCommandHandler
{
    private readonly IConfigRepository _configRepository = configRepository;
    public RedisCommand Command => RedisCommand.Get;
    public RespResponse Handle(RespRequest request)
    {
        if (request.Arguments.Count < 1)
            return RespResponse.FromError(request.Arguments.Count + " arguments must be at least 1");
        return RespResponse.FromBulkString(_configRepository.GetAsync(request.Arguments[0]).Result);
    }
    
}