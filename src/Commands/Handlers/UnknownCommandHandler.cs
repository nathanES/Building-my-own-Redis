using codecrafters_redis.Protocol;
using codecrafters_redis.RedisCommands;

namespace codecrafters_redis.Commands.Handlers;

public class UnknownCommandHandler : IRedisCommandHandler
{
    public RedisCommand Command => RedisCommand.Unknown;

    public Task<RespResponse> HandleAsync(RespRequest request)
    {
        Console.WriteLine($"Unknown Command received... : {request.Command}");
        return Task.FromResult(RespResponse.FromError("Unknown Command"));
    }
}