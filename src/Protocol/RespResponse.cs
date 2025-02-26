using System.Text;

namespace codecrafters_redis.Protocol;

internal class RespResponse
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