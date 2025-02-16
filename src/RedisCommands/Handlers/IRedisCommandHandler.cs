using codecrafters_redis.RespRequestResponse;

namespace codecrafters_redis.RedisCommands.Handlers;

public interface IRedisCommandHandler
{
    RedisCommand Command { get; }
    RespResponse Handle(RespRequest request);
}