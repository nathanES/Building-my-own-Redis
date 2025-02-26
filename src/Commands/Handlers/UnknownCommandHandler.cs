using codecrafters_redis.Protocol;

namespace codecrafters_redis.Commands.Handlers;

internal class UnknownCommandHandler : IRedisCommandHandler
{
    public RedisCommand Command => RedisCommand.Unknown;

    public Task<RespResponse> HandleAsync(string clientId, RespRequest request)
    {
        Console.WriteLine($"Unknown Command received... : {request.Command}");
        return Task.FromResult(RespResponse.FromError("Unknown Command"));
    }
}