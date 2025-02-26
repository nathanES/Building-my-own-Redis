using codecrafters_redis.Protocol;

namespace codecrafters_redis.Commands;

internal interface IRedisCommandHandler
{
    RedisCommand Command { get; }
    Task<RespResponse> HandleAsync(string clientId, RespRequest request);
}