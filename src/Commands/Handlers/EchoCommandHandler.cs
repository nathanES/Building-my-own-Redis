using codecrafters_redis.Protocol;

namespace codecrafters_redis.Commands.Handlers;

internal class EchoCommandHandler : IRedisCommandHandler
{
    public RedisCommand Command => RedisCommand.Echo;
    public Task<RespResponse> HandleAsync(string clientId, RespRequest request)
    {
        Console.WriteLine("[Debug] - Echo Command received...");
        return request.Arguments.Count > 0
            ? Task.FromResult(RespResponse.FromBulkString(request.Arguments.First()))
            : Task.FromResult(RespResponse.FromBulkString(string.Empty));
    }
}