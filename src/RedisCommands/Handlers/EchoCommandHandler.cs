using codecrafters_redis.RespRequestResponse;

namespace codecrafters_redis.RedisCommands.Handlers;

public class EchoCommandHandler : IRedisCommandHandler
{
    public RedisCommand Command => RedisCommand.Echo;
    public RespResponse Handle(RespRequest request)
    {
        Console.WriteLine("Echo Command received...");
        return request.Arguments.Count > 0  
            ? RespResponse.FromBulkString(request.Arguments.First())
            : RespResponse.FromBulkString(string.Empty);
    }
}