using codecrafters_redis.RedisRepositories;
using codecrafters_redis.RespRequestResponse;

namespace codecrafters_redis.RedisCommands.Handlers;

public class SetCommandHandler(IKeyValueRepository keyValueRepository) : IRedisCommandHandler
{
    private readonly IKeyValueRepository _keyValueRepository = keyValueRepository;
    public RedisCommand Command => RedisCommand.Set;

    public RespResponse Handle(RespRequest request)
    {
        var setCommandRequest = SetCommandParser.Parse(request.Arguments);
        if (setCommandRequest == null)
            return RespResponse.FromError("Invalid arguments provided.");

        _keyValueRepository.SetAsync(setCommandRequest.Key, setCommandRequest.Value, setCommandRequest.Expiry);
        return RespResponse.FromSimpleString("OK");
    }

    public record SetCommandRequest(string Key, string Value, TimeSpan? Expiry);

    public static class SetCommandParser
    {
        public static SetCommandRequest? Parse(IReadOnlyList<string> arguments)
        {
            if (arguments.Count < 2)
                return null;
            var key = arguments[0];
            var value = arguments[1];
            TimeSpan? expiry = null;
            for (int i = 2; i < arguments.Count; i++)
            {
                var arg = arguments[i].ToUpperInvariant();
                switch (arg)
                {
                    case "EX":
                        if (i + 1 >= arguments.Count || !int.TryParse(arguments[i + 1], out int seconds))
                            return null;
                        expiry = TimeSpan.FromSeconds(seconds);
                        break;
                    case "PX":
                        if (i + 1 >= arguments.Count || !int.TryParse(arguments[i + 1], out int miliseconds))
                            return null;
                        expiry = TimeSpan.FromMilliseconds(miliseconds);
                        break;
                }
            }

            return new SetCommandRequest(key, value, expiry);
        }
    }
}