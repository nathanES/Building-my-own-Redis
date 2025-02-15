using System.Text;

namespace codecrafters_redis;

public class RespRequest
{
    public RedisCommand Command { get; private set; } = RedisCommand.Unknown;
    public List<string> Arguments { get; private set; } = [];

    private static readonly Dictionary<string, RedisCommand> CommandLookup = new(StringComparer.OrdinalIgnoreCase)
    {
        { "PING", RedisCommand.Ping },
        // { "ECHO", RedisCommand.Echo }
    };

    public static RespRequest? Parse(byte[] rawRequest, int requestLength)
    {
        if (requestLength == 0 || rawRequest[0] != '*')
            return null;

        var index = 1;
        var numberOfArguments = ReadInteger(rawRequest, ref index);
        Console.WriteLine($"{nameof(numberOfArguments)}: {numberOfArguments})");
        RespRequestBuilder respRequestBuilder = new RespRequestBuilder()
            .AddCommand(ReadBulkString(rawRequest, ref index));
        for (var i = 1; i < numberOfArguments; i++)
            respRequestBuilder.AddArgument(ReadBulkString(rawRequest, ref index));
        return respRequestBuilder.Build();
    }

    private static int ReadInteger(byte[] rawRequest, ref int index)
    {
        int result = 0;
        bool negative = false;
        if (rawRequest[index] == '-')
        {
            negative = true;
            index++;
        }

        while (rawRequest[index] != '\r')
        {
            result = result * 10 + (rawRequest[index] - '0');
            index++;
        }

        index += 2; //skip \r\n
        return negative ? -1 * result : result;
    }

    private static string? ReadBulkString(byte[] rawRequest, ref int index)
    {
        if (rawRequest[index] != '$')
            return null;
        index++;
        int length = ReadInteger(rawRequest, ref index);

        string bulkString = Encoding.UTF8.GetString(rawRequest, index, length);
        index += length + 2; //Skip bulkString length and \r\n
        return bulkString;
    }

    public class RespRequestBuilder
    {
        private readonly RespRequest _respRequest = new();

        public RespRequestBuilder AddCommand(string? command)
        {
            if (command == null)
                return this;
            
            if (CommandLookup.TryGetValue(command, out var redisCommand))
                AddCommand(redisCommand);
            return this;
        }

        public RespRequestBuilder AddCommand(RedisCommand command)
        {
            _respRequest.Command = command;
            return this;
        }

        public RespRequestBuilder AddArguments(List<string> arguments)
        {
            foreach (var argument in arguments)
            {
                AddArgument(argument);
            }

            return this;
        }

        public RespRequestBuilder AddArguments(string[] arguments)
        {
            foreach (var argument in arguments)
            {
                AddArgument(argument);
            }

            return this;
        }

        public RespRequestBuilder AddArgument(string? argument)
        {
            if (argument == null)
                return this;
            
            _respRequest.Arguments.Add(argument);
            return this;
        }
        public RespRequest Build() => _respRequest;
    }
}

public class RespResponse
{
    public ReadOnlyMemory<byte> RawResponse { get; private set; }

    private RespResponse(ReadOnlyMemory<byte> rawResponse)
        => RawResponse = rawResponse;

    private static RespResponse BuildRespMessage(char prefix, string message)
    {
        int length = message.Length + 3;
        Span<byte> buffer = stackalloc byte[length];
        buffer[0] = (byte)prefix;
        Encoding.UTF8.GetBytes(message, buffer[1..]);
        buffer[^2] = (byte)'\r';
        buffer[^1] = (byte)'\n';
        return new RespResponse(buffer.ToArray());
    }

    public static RespResponse FromSimpleString(string @string)
        => BuildRespMessage('+', @string);

    public static RespResponse FromError(string error)
        => BuildRespMessage('-', error);

    public static RespResponse FromInteger(int integer)
        => BuildRespMessage(':', integer.ToString());

    public static RespResponse FromBulkString(string? bulkString)
        => bulkString is null
            ? BuildRespMessage('$', "-1")
            : BuildRespMessage('$', $"{bulkString.Length}\r\n{bulkString}");

    public static RespResponse FromArray(string[] array)
    {
        StringBuilder sb = new();
        sb.Append($"{array.Length}");
        foreach (var element in array)
            sb.Append($"\r\n${element.Length}\r\n{element}");
        return BuildRespMessage('*', sb.ToString());
    }

    public byte[] GetRawResponse() => RawResponse.ToArray();
}