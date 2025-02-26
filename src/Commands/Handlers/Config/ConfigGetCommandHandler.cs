using codecrafters_redis.Protocol;
using codecrafters_redis.RedisRepositories.Configuration;

namespace codecrafters_redis.Commands.Handlers.Config;

internal class ConfigGetCommandHandler(IRedisConfigRepository repository)
{
    private readonly IRedisConfigRepository _repository = repository;
    
    public async Task<RespResponse> HandleAsync(ConfigCommandRequest request)
    {
        if(request.Arguments.Count == 0 || string.IsNullOrWhiteSpace(request.Arguments[0]))
            return RespResponse.FromError("Not valid amount of arguments"); 
        
        var configValue = await _repository.GetAsync(request.Arguments[0]);
        return string.IsNullOrWhiteSpace(configValue) 
            ? RespResponse.FromBulkString(null) 
            : RespResponse.FromArray([request.Arguments[0], configValue]);
    }
}