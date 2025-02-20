using codecrafters_redis.Protocol;
using codecrafters_redis.RedisCommands;
using codecrafters_redis.RedisRepositories.KeyValue;

namespace codecrafters_redis.Commands.Handlers;

public class GetCommandHandler(IRedisKeyValueRepository redisKeyValueRepository) : IRedisCommandHandler
{
    private readonly IRedisKeyValueRepository _redisKeyValueRepository = redisKeyValueRepository;
    public RedisCommand Command => RedisCommand.Get;
    public Task<RespResponse> HandleAsync(RespRequest request)
    {
        if (request.Arguments.Count < 1)
            return Task.FromResult(RespResponse.FromError(request.Arguments.Count + " arguments must be at least 1"));
        return Task.FromResult(RespResponse.FromBulkString(_redisKeyValueRepository.GetAsync(request.Arguments[0]).Result));
    }
    
}