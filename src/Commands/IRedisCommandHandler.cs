using codecrafters_redis.Protocol;
using codecrafters_redis.RedisCommands;

namespace codecrafters_redis.Commands;

public interface IRedisCommandHandler
{
    RedisCommand Command { get; }
    Task<RespResponse> HandleAsync(RespRequest request);
}