using codecrafters_redis.Protocol;
using codecrafters_redis.RedisCommands;

namespace codecrafters_redis.Commands.Handlers;

public class PingCommandHandler : IRedisCommandHandler
{
    public RedisCommand Command => RedisCommand.Ping;
    public Task<RespResponse> HandleAsync(RespRequest request)
    {
        Console.WriteLine("Ping Command received...");
        return Task.FromResult(RespResponse.FromBulkString("PONG"));
    }
}