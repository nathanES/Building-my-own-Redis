using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using codecrafters_redis.Extensions;

namespace codecrafters_redis.RedisRepositories.Storage;

internal static class RdbFileConverter
{
    private const byte EndOfFile = 0xFF;
    private const byte DatabaseSelector = 0xFE;
    private const byte ExpireTime = 0xFD;
    private const byte ExpireTimeMs = 0xFC;
    private const byte ResizeDb = 0xFB;
    private const byte Auxiliary = 0xFA;


    public static  RdbFile? ParseToRdbFile(FileStream fileStream )
    {
        using BinaryReader reader = new(fileStream, Encoding.UTF8, leaveOpen: true);
        var rdbFileBuilder = new RdbFile.RdbFileBuilder();

        if (!TryValidateHeader(reader, out var version))
        {
            Console.WriteLine("Invalid Header");
            return null;
        }

        rdbFileBuilder.WithVersion(version);
        
        Console.WriteLine($"[Debug] Start Parsing Sections");
        ParseAuxiliarySection(reader, rdbFileBuilder);
        ParseDatabasesSection(reader, rdbFileBuilder);
        Console.WriteLine($"[Debug] End Parsing Sections");
        byte[] checksum = ReadChecksum(reader);
        bool isChecksumValid = ValidateChecksum(checksum);
        var rdbFile = rdbFileBuilder.WithChecksum(checksum, isChecksumValid).Build();
        Console.WriteLine($"[Debug] End Parsing File : {JsonSerializer.Serialize(rdbFile)}");
        return rdbFile;
    }
    
        private static void ParseAuxiliarySection(BinaryReader reader, RdbFile.RdbFileBuilder fileBuilder)
    {
        Console.WriteLine($"[Debug] Start {nameof(ParseAuxiliarySection)}");
        while (!reader.IsEndOfStream())
        {
            var @byte = reader.ReadByte();
            if (@byte != Auxiliary)
            {
                Console.WriteLine($"[Debug] End {nameof(ParseAuxiliarySection)}");
                reader.BaseStream.Position -= 1;
                break;
            }

            var key = ReadString(reader);
            Console.WriteLine($"[Debug] {nameof(key)} {key}");
            var value = ReadString(reader);
            Console.WriteLine($"[Debug] {nameof(value)} {value}");
            fileBuilder.AddAuxiliaryField(key, value);
        }
    }
    private static void ParseDatabasesSection(BinaryReader reader, RdbFile.RdbFileBuilder fileBuilder)
    {
        Console.WriteLine($"[Debug] Start {nameof(ParseDatabasesSection)}");
        while (!reader.IsEndOfStream())
        {
            var @byte = reader.ReadByte();
            if (@byte is EndOfFile or not DatabaseSelector)
                break; 

            ParseDatabase(reader, out var rdbDatabase);
            fileBuilder.AddDatabase(rdbDatabase); 
        }
    }
    private static void ParseDatabase(BinaryReader reader, out RdbDatabase rdbDatabase)
    {
        int currentDb = reader.ReadByte();
        Console.WriteLine($"[Debug] Switching to DB >{currentDb}<");

        var currentDbBuilder = new RdbDatabase.RdbDatabaseBuilder().WithDatabaseIndex(currentDb);
        
        Console.WriteLine($"[Debug] Start {nameof(ParseDatabase)}");
        var @byte = reader.ReadByte();
        Console.WriteLine($"[Debug] {nameof(@byte)} {@byte}");
        if (@byte != ResizeDb)
        {
            Console.WriteLine($"[Error] End {nameof(ParseDatabase)} : missing {nameof(ResizeDb)}");
            throw new Exception($"[Error] Expected {nameof(ResizeDb)} but got {@byte}");
        }

        var resizeDatabaseSize = ReadLength(reader);
        Console.WriteLine($"[Debug] {nameof(resizeDatabaseSize)} : {resizeDatabaseSize}");
        var resizeDatabaseExpiry = ReadLength(reader);
        Console.WriteLine($"[Debug] {nameof(resizeDatabaseExpiry)} : {resizeDatabaseExpiry}");
        DateTime? currentExpiration = null;
        for (var i = 0; i < resizeDatabaseSize ; i++)
        {
            switch (reader.ReadByte())
            {
                case ExpireTimeMs:
                    Console.WriteLine($"[Debug] {nameof(ExpireTimeMs)}");
                    var expireTimeMs = BitConverter.ToInt32(reader.ReadBytes(4));
                    currentExpiration = DateTime.UtcNow.AddMilliseconds(expireTimeMs);
                    break;
                case ExpireTime:
                    Console.WriteLine($"[Debug] {nameof(ExpireTime)}");
                    var expireTimeSeconds = BitConverter.ToInt32(reader.ReadBytes(8));
                    currentExpiration = DateTime.UtcNow.AddSeconds(expireTimeSeconds);
                    break;
                case 0x00:
                    var key = ReadString(reader);
                    Console.WriteLine($"[Debug] {nameof(key)} : {key}");
                    var value = ReadString(reader);
                    Console.WriteLine($"[Debug] {nameof(value)} : {value}");
                    var entry = new RdbEntry.RdbEntryBuilder()
                        .WithKey(key)
                        .WithValue(Encoding.UTF8.GetBytes(value))
                        .WithExpiry(currentExpiration)
                        .Build();
                    Console.WriteLine("[Debug] Adding entry");
                    currentDbBuilder.AddEntry(entry); 
                    currentExpiration = null;
                    break;
                default:
                    throw new NotSupportedException($"[Error] Unexpected {nameof(ExpireTimeMs)}.");
            }
        }
        rdbDatabase = currentDbBuilder.Build();
    }

    
    private static string ReadString(BinaryReader reader)
    {
        Console.WriteLine($"[Debug] Start {nameof(ReadString)}");
        var (isStringEncoded, stringLength) = GetStringLengthEncoding(reader);
        Console.WriteLine($"[Debug] {nameof(isStringEncoded)} {isStringEncoded}");
        Console.WriteLine($"[Debug] {nameof(stringLength)} {stringLength}");
        
        if (isStringEncoded) 
            return Encoding.UTF8.GetString(reader.ReadBytes(stringLength));
        
        //Stringed Integer
        var (isInt, intValue) = GetInt(reader);
        Console.WriteLine($"[Debug] {nameof(isInt)} {isInt}");
        Console.WriteLine($"[Debug] {nameof(intValue)} {intValue}");
        if (isInt)
            return intValue.ToString();
        //TODO
        throw new InvalidDataException($"[Error] Compressed String not yet Handled");


    }

    private static int ReadLength(BinaryReader reader)
    {
        Console.WriteLine($"[Debug] Start {nameof(ReadLength)}");
        var (isStringEncoded, stringLength) = GetStringLengthEncoding(reader);
        Console.WriteLine($"[Debug] {nameof(isStringEncoded)} {isStringEncoded}");
        Console.WriteLine($"[Debug] {nameof(stringLength)} {stringLength}");
        if(!isStringEncoded)
            throw new InvalidDataException($"[Error] Cannot Read Length");
        return stringLength;
        
    }

    private static (bool isInt, int value) GetInt(BinaryReader reader)
    {
        Console.WriteLine($"[Debug] Start {nameof(GetInt)}");

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
        Console.WriteLine($"[Debug] Start {nameof(GetStringLengthEncoding)}");
        var lengthEncoding = reader.ReadByte();
        var lengthEncodingType = (lengthEncoding & 0b1100_0000) >> 6;
        Console.WriteLine($"[Debug] {nameof(lengthEncodingType)} : {lengthEncodingType}");
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