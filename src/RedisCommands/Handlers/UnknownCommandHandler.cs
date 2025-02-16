using codecrafters_redis.RespRequestResponse;

namespace codecrafters_redis.RedisCommands.Handlers;

public class UnknownCommandHandler : IRedisCommandHandler
{
    public RedisCommand Command => RedisCommand.Unknown;

    public RespResponse Handle(RespRequest request)
    {
        Console.WriteLine($"Unknown Command received... : {request.Command}");
        return RespResponse.FromError("Unknown Command");
    }
}