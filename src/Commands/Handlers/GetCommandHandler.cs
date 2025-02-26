using codecrafters_redis.Protocol;
using codecrafters_redis.RedisRepositories.Storage;

namespace codecrafters_redis.Commands.Handlers;

internal class GetCommandHandler(IRedisStorageRepository redisStorageRepository) : IRedisCommandHandler
{
    private readonly IRedisStorageRepository _redisStorageRepository = redisStorageRepository;
    public RedisCommand Command => RedisCommand.Get;
    public Task<RespResponse> HandleAsync(string clientId, RespRequest request)
    {
        if (request.Arguments.Count < 1)
            return Task.FromResult(RespResponse.FromError(request.Arguments.Count + " arguments must be at least 1"));
        return Task.FromResult(RespResponse.FromBulkString(_redisStorageRepository.GetAsync(clientId, request.Arguments[0]).Result));
    }
    
}