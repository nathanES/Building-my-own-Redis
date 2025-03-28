using codecrafters_redis.Protocol;

namespace codecrafters_redis.Commands.Handlers.Config;

internal class ConfigCommandHandler(ConfigGetCommandHandler configGetCommandHandler) : IRedisCommandHandler
{
    private readonly ConfigGetCommandHandler _configGetCommandHandler = configGetCommandHandler;
    public RedisCommand Command => RedisCommand.Config;

    public async Task<RespResponse> HandleAsync(string clientId, RespRequest request)
    {
        var configRequest = ConfigCommandRequest.Parse(request.Arguments);
        if (configRequest == null)
            return RespResponse.FromError("Invalid arguments provided.");
        switch (configRequest.Command)
        {
            case ConfigCommand.Get:
                return await _configGetCommandHandler.HandleAsync(configRequest);
            case ConfigCommand.Unknown:
            default:
                return RespResponse.FromError("Unknown Command for config");
        }
    }

    

   
}