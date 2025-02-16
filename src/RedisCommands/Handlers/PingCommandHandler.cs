using codecrafters_redis.RespRequestResponse;

namespace codecrafters_redis.RedisCommands.Handlers;

public class PingCommandHandler : IRedisCommandHandler
{
    public RedisCommand Command => RedisCommand.Ping;
    public RespResponse Handle(RespRequest request)
    {
        Console.WriteLine("Ping Command received...");
        return RespResponse.FromBulkString("PONG");
    }
}