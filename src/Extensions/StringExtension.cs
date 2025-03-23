using System.Text.RegularExpressions;

namespace codecrafters_redis.Extensions;

internal static class StringExtension
{
    public static Regex CreateRegex(this string pattern, RegexOptions options = RegexOptions.None)
        => new Regex(pattern, options);
}