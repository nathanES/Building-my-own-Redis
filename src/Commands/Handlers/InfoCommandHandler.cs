using codecrafters_redis.Protocol;

namespace codecrafters_redis.Commands.Handlers;

internal class InfoCommandHandler : IRedisCommandHandler
{
    public RedisCommand Command => RedisCommand.Info;
    public Task<RespResponse> HandleAsync(string clientId, RespRequest request)
    {
        var response = "role:master";
        return Task.FromResult(RespResponse.FromBulkString(response));
    }
}