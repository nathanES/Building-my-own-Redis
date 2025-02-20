using codecrafters_redis.Protocol;
using codecrafters_redis.RedisCommands;

namespace codecrafters_redis.Commands.Handlers;

public class EchoCommandHandler : IRedisCommandHandler
{
    public RedisCommand Command => RedisCommand.Echo;
    public Task<RespResponse> HandleAsync(RespRequest request)
    {
        Console.WriteLine("Echo Command received...");
        return request.Arguments.Count > 0
            ? Task.FromResult(RespResponse.FromBulkString(request.Arguments.First()))
            : Task.FromResult(RespResponse.FromBulkString(string.Empty));
    }
}