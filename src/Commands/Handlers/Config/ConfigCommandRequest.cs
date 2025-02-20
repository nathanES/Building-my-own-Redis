namespace codecrafters_redis.Commands.Handlers.Config;

public class ConfigCommandRequest
{
    public ConfigCommand Command { get; private set; } = ConfigCommand.Unknown;
    public List<string> Arguments { get; private set; } = [];
    private static readonly Dictionary<string, ConfigCommand> CommandLookup = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Get", ConfigCommand.Get },
    };

    public static ConfigCommandRequest? Parse(List<string> arguments)
    {
        var argumentsCount = arguments.Count;
        if(argumentsCount < 1 || !CommandLookup.TryGetValue(arguments[0], out var command))
            return null;
            
        return new ConfigCommandRequest()
        {
            Command = command,
            Arguments = argumentsCount > 1 ? arguments[1..] : [],
        };
    }
}