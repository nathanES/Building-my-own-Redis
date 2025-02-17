using codecrafters_redis.RedisRepositories;
using codecrafters_redis.RespRequestResponse;

namespace codecrafters_redis.RedisCommands.Handlers;

public class GetCommandHandler(IKeyValueRepository keyValueRepository) : IRedisCommandHandler
{
    private readonly IKeyValueRepository _keyValueRepository = keyValueRepository;
    public RedisCommand Command => RedisCommand.Get;
    public RespResponse Handle(RespRequest request)
    {
        if (request.Arguments.Count < 1)
            return RespResponse.FromError(request.Arguments.Count + " arguments must be at least 1");
        return RespResponse.FromBulkString(_keyValueRepository.GetAsync(request.Arguments[0]).Result);
    }
    
}