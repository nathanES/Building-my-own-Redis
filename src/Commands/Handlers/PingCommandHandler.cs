using codecrafters_redis.Protocol;

namespace codecrafters_redis.Commands.Handlers;

internal class PingCommandHandler : IRedisCommandHandler
{
    public RedisCommand Command => RedisCommand.Ping;
    public Task<RespResponse> HandleAsync(string clientId, RespRequest request)
    {
        Console.WriteLine("[Debug] - Ping Command received...");
        return Task.FromResult(RespResponse.FromBulkString("PONG"));
    }
}
