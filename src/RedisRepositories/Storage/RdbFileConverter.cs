using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using codecrafters_redis.Extensions;

namespace codecrafters_redis.RedisRepositories.Storage;

internal static class RdbFileConverter
{
    private const byte EndOfFile = 0xFF;
    private const byte DatabaseSelector = 0xFE;
    private const byte ExpireTimeSec = 0xFD;
    private const byte ExpireTimeMs = 0xFC;
    private const byte ResizeDb = 0xFB;
    private const byte Auxiliary = 0xFA;


    public static async Task<RdbFile?> ParseToRdbFileAsync(FileStream fileStream)
    {
        using BinaryReader reader = new(fileStream, Encoding.UTF8, leaveOpen: true);
        var rdbFileBuilder = new RdbFile.RdbFileBuilder();

        if (!TryValidateHeader(reader, out var version))
        {
            Console.WriteLine("[Debug] - Invalid Header");
            return null;
        }

        rdbFileBuilder.WithVersion(version);

        Console.WriteLine($"[Debug] - Start Parsing Sections");
        await ParseAuxiliarySectionAsync(reader, rdbFileBuilder);
        await ParseDatabasesSectionAsync(reader, rdbFileBuilder);
        Console.WriteLine($"[Debug] - End Parsing Sections");

        byte[] checksum = ReadChecksum(reader);
        bool isChecksumValid = ValidateChecksum(checksum);
        var rdbFile = rdbFileBuilder.WithChecksum(checksum, isChecksumValid).Build();
        Console.WriteLine($"[Debug] - End Parsing File : {JsonSerializer.Serialize(rdbFile)}");
        return rdbFile;
    }

    private static Task ParseAuxiliarySectionAsync(BinaryReader reader, RdbFile.RdbFileBuilder fileBuilder)
    {
        Console.WriteLine($"[Debug] - Start {nameof(ParseAuxiliarySectionAsync)}");
        while (!reader.IsEndOfStream())
        {
            var @byte = reader.ReadByte();
            if (@byte != Auxiliary)
            {
                Console.WriteLine($"[Debug] - End {nameof(ParseAuxiliarySectionAsync)}");
                reader.BaseStream.Position -= 1;
                break;
            }

            var key = ReadString(reader);
            Console.WriteLine($"[Debug] - {nameof(key)} {key}");
            var value = ReadString(reader);
            Console.WriteLine($"[Debug] - {nameof(value)} {value}");
            fileBuilder.AddAuxiliaryField(key, value);
        }

        return Task.CompletedTask;
    }

    private static async Task ParseDatabasesSectionAsync(BinaryReader reader, RdbFile.RdbFileBuilder fileBuilder)
    {
        Console.WriteLine($"[Debug] - Start {nameof(ParseDatabasesSectionAsync)}");
        while (!reader.IsEndOfStream())
        {
            var @byte = reader.ReadByte();
            if (@byte is EndOfFile or not DatabaseSelector)
                break;

            await ParseDatabaseAsync(reader, out var rdbDatabase);
            fileBuilder.AddDatabase(rdbDatabase);
        }
    }

    private static Task ParseDatabaseAsync(BinaryReader reader, out RdbDatabase rdbDatabase)
    {
        int currentDb = reader.ReadByte();
        Console.WriteLine($"[Debug] - Switching to DB >{currentDb}<");

        var currentDbBuilder = new RdbDatabase.RdbDatabaseBuilder().WithDatabaseIndex(currentDb);

        Console.WriteLine($"[Debug] - Start {nameof(ParseDatabaseAsync)}");
        var resizeDBByte = reader.ReadByte();
        Console.WriteLine($"[Debug] - {nameof(resizeDBByte)} {resizeDBByte}");
        if (resizeDBByte != ResizeDb)
        {
            Console.WriteLine($"[Error] - End {nameof(ParseDatabaseAsync)} : missing {nameof(ResizeDb)}");
            throw new Exception($"[Error] Expected {nameof(ResizeDb)} but got {resizeDBByte}");
        }

        var resizeDatabaseSize = ReadLength(reader);
        Console.WriteLine($"[Debug] - {nameof(resizeDatabaseSize)} : {resizeDatabaseSize}");
        var resizeDatabaseExpiry = ReadLength(reader);
        Console.WriteLine($"[Debug] - {nameof(resizeDatabaseExpiry)} : {resizeDatabaseExpiry}");
        for (var i = 0; i < resizeDatabaseSize; i++)
        {
            var @byte = reader.ReadByte();
            switch (@byte)
            {
                case ExpireTimeMs :
                case ExpireTimeSec:
                    Console.WriteLine($"[Debug] - {@byte:X2} - Expiry Entry");
                    var expiryMilliseconds = @byte == ExpireTimeMs 
                        ? (long)BitConverter.ToUInt64(reader.ReadBytes(8))
                        : BitConverter.ToUInt32(reader.ReadBytes(4)) * 1000L; //L say to the compiler that 1000 is a long and not an integer
                    if (reader.ReadByte() != 0x00) //We only handle classical string for the moment
                        continue;
                    Console.WriteLine("[Debug] - Adding entry");
                    currentDbBuilder.AddEntry(new RdbEntry.RdbEntryBuilder()
                        .WithKeyValue(ReadKeyValue(reader))
                        .WithExpiry(ConvertToDateTimeExpiry(expiryMilliseconds))
                        .Build());
                    break;
                case 0x00:
                    Console.WriteLine("[Debug] - Key Value Entry");
                    currentDbBuilder.AddEntry(new RdbEntry.RdbEntryBuilder()
                        .WithKeyValue(ReadKeyValue(reader))
                        .Build());
                    break;
                default:
                    continue;
            }
        }

        rdbDatabase = currentDbBuilder.Build();
        return Task.CompletedTask;
    }

    private static (string Key, byte[] Value) ReadKeyValue(BinaryReader reader)
    {
        var key = ReadString(reader);
        Console.WriteLine($"[Debug] - {nameof(key)} : {key}");
        var value = ReadString(reader);
        Console.WriteLine($"[Debug] - {nameof(value)} : {value}");
        return (key, Encoding.UTF8.GetBytes(value));
    }

    private static DateTime ConvertToDateTimeExpiry(long milliseconds)
        => DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).UtcDateTime;

    private static string ReadString(BinaryReader reader)
    {
        Console.WriteLine($"[Debug] - Start {nameof(ReadString)}");
        var (isStringEncoded, stringLength) = GetStringLengthEncoding(reader);
        Console.WriteLine($"[Debug] - {nameof(isStringEncoded)} {isStringEncoded}");
        Console.WriteLine($"[Debug] - {nameof(stringLength)} {stringLength}");

        if (isStringEncoded)
            return Encoding.UTF8.GetString(reader.ReadBytes(stringLength));

        //Stringed Integer
        var (isInt, intValue) = GetInt(reader);
        Console.WriteLine($"[Debug] - {nameof(isInt)} {isInt}");
        Console.WriteLine($"[Debug] - {nameof(intValue)} {intValue}");
        if (isInt)
            return intValue.ToString();
        //TODO
        throw new InvalidDataException($"[Error] Compressed String not yet Handled");
    }

    private static int ReadLength(BinaryReader reader)
    {
        Console.WriteLine($"[Debug] - Start {nameof(ReadLength)}");
        var (isStringEncoded, stringLength) = GetStringLengthEncoding(reader);
        Console.WriteLine($"[Debug] - {nameof(isStringEncoded)} {isStringEncoded}");
        Console.WriteLine($"[Debug] - {nameof(stringLength)} {stringLength}");
        if (!isStringEncoded)
            throw new InvalidDataException($"[Error] Cannot Read Length");
        return stringLength;
    }

    private static (bool isInt, int value) GetInt(BinaryReader reader)
    {
        Console.WriteLine($"[Debug] - Start {nameof(GetInt)}");

        switch (reader.ReadByte())
        {
            case 0xC0:
                return (true, reader.ReadByte());
            case 0xC1: //16bits integer
                return (true, reader.ReadBytes(2).ConvertToIntBigEndian());
            case 0xC2: //32bits integer
                return (true, reader.ReadBytes(4).ConvertToIntBigEndian());
            default:
                reader.BaseStream.Position -= 1;
                return (false, 0);
        }
    }

    private static (bool isStringEncoded, int stringLength) GetStringLengthEncoding(BinaryReader reader)
    {
        Console.WriteLine($"[Debug] - Start {nameof(GetStringLengthEncoding)}");
        var lengthEncoding = reader.ReadByte();
        var lengthEncodingType = (lengthEncoding & 0b1100_0000) >> 6;
        Console.WriteLine($"[Debug] - {nameof(lengthEncodingType)} : {lengthEncodingType}");
        switch (lengthEncodingType)
        {
            case 0b00:
                return (true, lengthEncoding & 0b0011_1111);
            case 0b01:
                return (true, (lengthEncoding & 0b0011_1111) << 8 + reader.ReadByte());
            case 0b10:
                return (true, BitConverter.ToInt32(reader.ReadBytes(4)));
            case 0b11:
            default:
                reader.BaseStream.Position -= 1;
                return (false, 0);
        }
    }

    private static bool TryValidateHeader(BinaryReader reader, out int redisVersion)
    {
        redisVersion = 0;
        var magicString = Encoding.UTF8.GetString(reader.ReadBytes(5));
        if (magicString != "REDIS")
            return false;

        if (!int.TryParse(reader.ReadBytes(4), out var version))
            return false;
        redisVersion = version;
        return true;
    }

    private static byte[] ReadChecksum(BinaryReader reader)
    {
        if (reader.BaseStream.Length < 8) return new byte[8];

        reader.BaseStream.Seek(-8, SeekOrigin.End);
        return reader.ReadBytes(8);
    }

    private static bool ValidateChecksum(byte[] checksum)
    {
        return true; // TODO: Implement CRC64 checksum validation
    }
}