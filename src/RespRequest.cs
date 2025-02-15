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
        if (requestLength == 0 || rawRequest[0]!='*')
            return null;
        var requestBuilder = new RespRequestBuilder();//TODO to update
        var request = Encoding.UTF8.GetString(rawRequest, 0, requestLength);
        requestBuilder.AddCommand(request);
        throw new NotImplementedException(nameof(RespRequest.Parse));
    }

    public class RespRequestBuilder
    {
        private readonly RespRequest _respRequest = new ();

        public RespRequestBuilder AddCommand(string command)
        {
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
        public RespRequestBuilder AddArgument(string argument)
        {
            _respRequest.Arguments.Add(argument);
            return this;
        }
        
        
    }
}

public class RespResponse
{
    public ReadOnlyMemory<byte> RawResponse { get; private set; }

    private RespResponse(ReadOnlyMemory<byte> rawResponse) 
        => RawResponse = rawResponse;

    private static RespResponse CreateResponse(char prefix, string message)
    {
        int length = message.Length + 3;
        Span<byte> buffer = stackalloc byte[length];
        buffer[0] = (byte)prefix;
        Encoding.UTF8.GetBytes(message, buffer[1..]);
        buffer[^2] = (byte)'\r';
        buffer[^1] = (byte)'\n';
        return new RespResponse(buffer.ToArray());
    }

    public static RespResponse SimpleString(string @string)
        => CreateResponse('+', @string);
    public static RespResponse SimpleError(string error)
        => CreateResponse('-', error);
    public static RespResponse Integers(int integers)
        => CreateResponse(':', integers.ToString());

    public static RespResponse BulkString(string? bulkString)
        => bulkString is null 
            ? CreateResponse('$', "-1") 
            : CreateResponse('$', $"{bulkString.Length}\r\n{bulkString}");

    public static RespResponse Array(string[] array)
    {
        StringBuilder sb = new();
        sb.Append($"{array.Length}");
        foreach (var element in array)
            sb.Append($"\r\n${element.Length}\r\n{element}");
        return CreateResponse('*', sb.ToString()); 
    }
    
    public byte[] GetRawResponse() => RawResponse.ToArray();
}