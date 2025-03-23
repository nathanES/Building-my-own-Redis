using codecrafters_redis.Protocol;
using codecrafters_redis.RedisRepositories.Storage;

namespace codecrafters_redis.Commands.Handlers;

internal class GetCommandHandler(IRedisStorageRepository redisStorageRepository) : IRedisCommandHandler
{
    private readonly IRedisStorageRepository _redisStorageRepository = redisStorageRepository;
    public RedisCommand Command => RedisCommand.Get;
    public async Task<RespResponse> HandleAsync(string clientId, RespRequest request)
    {
        if (request.Arguments.Count < 1)
            return RespResponse.FromError(request.Arguments.Count + " arguments must be at least 1");

        var getResult = await _redisStorageRepository.GetByKeyPatternAsync(clientId, key => request.Arguments[0] == key); 
        return RespResponse.FromBulkString(getResult.FirstOrDefault().Value);
    }
    
}