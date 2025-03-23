using System.Text.RegularExpressions;
using codecrafters_redis.Extensions;
using codecrafters_redis.Protocol;
using codecrafters_redis.RedisRepositories.Storage;

namespace codecrafters_redis.Commands.Handlers;

internal class KeysCommandHandler(IRedisStorageRepository redisStorageRepository) : IRedisCommandHandler
{
    private readonly IRedisStorageRepository _redisStorageRepository = redisStorageRepository;
    public RedisCommand Command => RedisCommand.Keys;
    public async Task<RespResponse> HandleAsync(string clientId, RespRequest request)
    {
        if (request.Arguments.Count < 1)
            return RespResponse.FromError(request.Arguments.Count + " arguments must be at least 1");
        
        Regex regex = CreateRegex(request.Arguments[0]);

        var getResult = await _redisStorageRepository.GetByKeyPatternAsync(clientId, key => regex.IsMatch(key));
        return RespResponse.FromArray(getResult.Select(x=>x.Key).ToArray());
    }

    private Regex CreateRegex(string pattern)
    {
        string regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*", ".*") // `*` matches any number of characters
            .Replace("\\?", ".") // `?` matches exactly one character
            .Replace("\\[", "[") // `[abc]` matches one of the listed characters
            .Replace("\\]", "]");
        return regexPattern.CreateRegex(RegexOptions.Compiled| RegexOptions.IgnoreCase);
    }
}