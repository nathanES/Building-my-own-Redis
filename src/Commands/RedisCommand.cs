namespace codecrafters_redis.Commands;

internal enum RedisCommand
{
    Unknown,
    Ping,
    Echo,
    Set,
    Get,
    Config,
    Keys,
    Info
}