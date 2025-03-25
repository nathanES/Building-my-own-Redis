using codecrafters_redis.Protocol;
using codecrafters_redis.RedisRepositories.Configuration;

namespace codecrafters_redis.Commands.Handlers;

internal class InfoCommandHandler (IRedisConfigRepository configRepository): IRedisCommandHandler
{
    public RedisCommand Command => RedisCommand.Info;
    public async Task<RespResponse> HandleAsync(string clientId, RespRequest request)
    {
        var response = new RedisInfo()
        {
            Role = string.IsNullOrWhiteSpace(await configRepository.GetAsync(ConstantsConfigurationKeys.Replicaof))
                ? "master"
                : "slave",
            MasterReplicationId = Guid.NewGuid().ToString("N"),
            MasterReplicationOffset = 0
        };
        return RespResponse.FromBulkString(string.Join(",\r\n",response.RespResponseFormated()));
    }
}

internal class RedisInfo
{
    public string Role {get; init;}
    public string MasterReplicationId { get; init; } = Guid.NewGuid().ToString("N");
    public int MasterReplicationOffset { get; init; } = 0;

    public IEnumerable<string> RespResponseFormated()
    {
        yield return $"role:{Role}";
        yield return $"master_replid:{MasterReplicationId}";
        yield return $"master_repl_offset:{MasterReplicationOffset}";
    }
}