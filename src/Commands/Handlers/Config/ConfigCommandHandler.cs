using codecrafters_redis.Protocol;
using codecrafters_redis.RedisCommands;

namespace codecrafters_redis.Commands.Handlers.Config;

public class ConfigCommandHandler(ConfigGetCommandHandler configGetCommandHandler) : IRedisCommandHandler
{
    private readonly ConfigGetCommandHandler _configGetCommandHandler = configGetCommandHandler;
    public RedisCommand Command => RedisCommand.Config;

    public async Task<RespResponse> HandleAsync(RespRequest request)
    {
        var configRequest = ConfigCommandRequest.Parse(request.Arguments);
        if (configRequest == null)
            return RespResponse.FromError("Invalid arguments provided.");
        switch (configRequest.Command)
        {
            case ConfigCommand.Get:
                return await _configGetCommandHandler.HandleAsync(configRequest);
                break;
            case ConfigCommand.Unknown:
            default:
                return RespResponse.FromError("Unknown Command for config");
        }
    }

    

   
}