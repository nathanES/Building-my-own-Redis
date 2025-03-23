using System.Text.Json;
using System.Text.Json.Nodes;
using codecrafters_redis.Extensions;
using codecrafters_redis.Protocol;
using codecrafters_redis.RedisRepositories.Configuration;

namespace codecrafters_redis.Commands.Handlers;

internal class InfoCommandHandler (IRedisConfigRepository configRepository): IRedisCommandHandler
{
    public RedisCommand Command => RedisCommand.Info;
    public async Task<RespResponse> HandleAsync(string clientId, RespRequest request)
    {
        var response = string.IsNullOrWhiteSpace(await configRepository.GetAsync(ConstantsConfigurationKeys.Replicaof) )? "role:master" : "role:slave";
        return RespResponse.FromBulkString(response);
    }
}