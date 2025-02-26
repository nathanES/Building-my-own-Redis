using codecrafters_redis.Protocol;

namespace codecrafters_redis.Commands.Handlers;

internal class KeysCommandHandler : IRedisCommandHandler
{
    public RedisCommand Command => RedisCommand.Keys;
    public async Task<RespResponse> HandleAsync(string clientId, RespRequest request)
    {
        //TODO to continue
        throw new NotImplementedException();
    }
}