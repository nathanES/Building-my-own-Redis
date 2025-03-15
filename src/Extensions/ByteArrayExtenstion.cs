namespace codecrafters_redis.Extensions;

internal static class ByteArrayExtenstion
{
   public static int ConvertToIntBigEndian(this byte[] bytes)
   {
       Span<byte> span = stackalloc byte[bytes.Length];
       bytes.CopyTo(span);
       span.Reverse();
       return BitConverter.ToInt32(span);
   }
}