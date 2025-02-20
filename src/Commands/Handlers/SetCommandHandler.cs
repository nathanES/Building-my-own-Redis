using codecrafters_redis.Protocol;
using codecrafters_redis.RedisCommands;
using codecrafters_redis.RedisRepositories.KeyValue;

namespace codecrafters_redis.Commands.Handlers;

public class SetCommandHandler(IRedisKeyValueRepository repository) : IRedisCommandHandler
{
    private readonly IRedisKeyValueRepository _repository = repository;
    public RedisCommand Command => RedisCommand.Set;

    public Task<RespResponse> HandleAsync(RespRequest request)
    {
        var setCommandRequest = SetCommandParser.Parse(request.Arguments);
        if (setCommandRequest == null)
            return Task.FromResult(RespResponse.FromError("Invalid arguments provided."));

        _repository.SetAsync(setCommandRequest.Key, setCommandRequest.Value, setCommandRequest.Expiry);
        return Task.FromResult(RespResponse.FromSimpleString("OK"));
    }

    private record SetCommandRequest(string Key, string Value, TimeSpan? Expiry);

    private static class SetCommandParser
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