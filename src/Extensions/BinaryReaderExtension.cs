namespace codecrafters_redis.Extensions;

internal static class BinaryReaderExtension
{
    public static bool IsEndOfStream(this BinaryReader reader)
        => reader.BaseStream.Position >= reader.BaseStream.Length;
}