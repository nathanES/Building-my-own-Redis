using System.Buffers;
using System.Text;
using codecrafters_redis.CompressionAlgorithm;

namespace codecrafters_redis.RedisRepositories.Storage;

internal static class RdbFileConverter
{
    private const byte EndOfFile = 0xFF;
    private const byte DatabaseSelector = 0xFE;
    private const byte ExpireTime = 0xFD;
    private const byte ExpireTimeMs = 0xFC;
    private const byte ResizeDb = 0xFB;
    private const byte Auxiliary = 0xFA;


    public static RdbFile? Parse(FileStream fileStream)
    {
        using BinaryReader reader = new(fileStream, Encoding.ASCII, leaveOpen: true);
        if (!ValidateHeader(reader))
        {
            Console.WriteLine("Invalid Header");
            return null;
        }

        var rdbFileBuilder = new RdbFile.RdbFileBuilder();
        var databaseBuilders = new Dictionary<int, RdbDatabase.RdbDatabaseBuilder>();

        while (reader.BaseStream.Position < reader.BaseStream.Length - 8) // Exclude checksum
        {
            byte currentByte = reader.ReadByte();
            Console.WriteLine($"{nameof(currentByte)} : 0x{currentByte:X2}<");
            if (currentByte == EndOfFile)
            {
                Console.WriteLine("End of File");
                break;
            }

            switch (currentByte)
            {
                case DatabaseSelector:
                    Console.WriteLine("Database Selector");
                    ParseDatabaseSelector(reader, databaseBuilders);
                    break;


                case Auxiliary:
                    Console.WriteLine("Auxiliary");
                    var auxKey = ReadString(reader).ToString();
                    var auxValue = ReadString(reader).ToString();
                    // rdbFileBuilder.AddAuxiliaryField(auxKey, auxValue);
                    Console.WriteLine($"Auxiliary field: {auxKey} = {auxValue}");
                    break;

                default:
                    Console.WriteLine($"Unknown Byte {currentByte}");
                    break;
            }
        }

        //Construct databases
        foreach (var db in databaseBuilders.Values)
        {
            rdbFileBuilder.AddDatabase(db.Build());
        }

        byte[] checksum = ReadChecksum(reader);
        bool isChecksumValid = ValidateChecksum(checksum);

        return rdbFileBuilder.WithChecksum(checksum, isChecksumValid).Build();
    }

    private static void ParseDatabaseSelector(BinaryReader reader,
        Dictionary<int, RdbDatabase.RdbDatabaseBuilder> databaseBuilders)
    {
        DateTime? currentExpiration = null;
        int currentDb = reader.ReadByte(); // The first byte after DatabaseSelector is the DB index
        Console.WriteLine($"Switching to DB >{currentDb}<");

        if (!databaseBuilders.TryGetValue(currentDb, out var currentDbBuilder))
        {
            currentDbBuilder = new RdbDatabase.RdbDatabaseBuilder().WithDatabaseIndex(currentDb);
            databaseBuilders[currentDb] = currentDbBuilder;
        }

        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            byte dataType = reader.ReadByte();
            Console.WriteLine($"[Debug] DataType: 0x{dataType:X2}");

            switch (dataType)
            {
                case EndOfFile:
                    Console.WriteLine("End of file");
                    return;

                case DatabaseSelector:
                    Console.WriteLine("Database Selector");
                    ParseDatabaseSelector(reader, databaseBuilders);
                    return;

                case ExpireTime:
                    Console.WriteLine("Expire Time");
                    var expireSeconds = reader.ReadInt32();
                    currentExpiration = DateTime.UtcNow.AddSeconds(expireSeconds);
                    Console.WriteLine($"Expiration (seconds): {expireSeconds}");
                    break;

                case ExpireTimeMs:
                    Console.WriteLine("Expire Time Milliseconds");
                    var expireMs = reader.ReadInt64();
                    currentExpiration = DateTime.UtcNow.AddMilliseconds(expireMs);
                    Console.WriteLine($"Expiration (milliseconds): {expireMs}");
                    break;

                case ResizeDb:
                    Console.WriteLine("Resize DB");
                    int hashTableSize = reader.ReadInt32();
                    int expiryTableSize = reader.ReadInt32();
                    Console.WriteLine($"Resizing DB: hashTableSize={hashTableSize}, expiryTableSize={expiryTableSize}");

                    // Peek at the next byte to verify it's a valid object type before proceeding
                    byte nextByte = (byte)reader.PeekChar();
                    Console.WriteLine($"[Debug] Next byte after ResizeDb: {nextByte:X2}");
                    break;

                case 0x00: // String Key-Value Pair
                case 0x01: // Linked List
                case 0x02: // Set
                case 0x03: // Sorted Set
                case 0x04: // Hash Map
                    Console.WriteLine("[Debug] Found key-value pair.");
                    string key = ReadString(reader).ToString();
                    object value = ReadString(reader);
                    Console.WriteLine($"[Debug] Key: {key}, Value: {value}");

                    if (currentDbBuilder == null)
                    {
                        currentDbBuilder = new RdbDatabase.RdbDatabaseBuilder().WithDatabaseIndex(currentDb);
                        databaseBuilders[currentDb] = currentDbBuilder;
                    }

                    var entry = new RdbEntry.RdbEntryBuilder()
                        .WithKey(key)
                        .WithValue(
                            value is string str ? Encoding.UTF8.GetBytes(str) : BitConverter.GetBytes((int)value))
                        .WithExpiry(currentExpiration)
                        .Build();

                    currentDbBuilder!.AddEntry(entry);
                    currentExpiration = null; // Reset expiration for next key
                    break;

                default:
                    Console.WriteLine($"[Error] Unknown Data Type 0x{dataType:X2}");
                    return; // Stop parsing if we reach an unknown type
            }
        }
    }


    private static bool ValidateHeader(BinaryReader reader)
    {
        byte[] header = reader.ReadBytes(9);
        return Encoding.ASCII.GetString(header) == "REDIS0011";
    }

    private static string ParseStringEncoded(BinaryReader reader)
    {
        int size = ParseSizeEncoded(reader);
        Console.WriteLine("Size : " + size);
        byte[] stringData = new byte[size];
        reader.Read(stringData, 0, size);
        return Encoding.UTF8.GetString(stringData);
    }

    private static int ParseSizeEncoded(BinaryReader reader)
    {
        byte firstByte = reader.ReadByte();
        switch ((firstByte & 0xC0)) //1100 0000
        {
            case 0x00:
                //The next six bits is the length
                Console.WriteLine("0x00");
                return firstByte & 0x3F; //0011 1111
            case 0x40:
                //Read the next byte and combine the 14 bits
                Console.WriteLine("0x40");
                if (reader.BaseStream.Position >= reader.BaseStream.Length)
                    throw new EndOfStreamException(
                        "[Error] ReadLength: Expected another byte but reached end of stream.");

                return ((firstByte & 0x3F) << 8) | reader.ReadByte();
            case 0x80:
                //Discard the 6 bits and the 4 next bytes is the length
                Console.WriteLine("0x80");
                if (reader.BaseStream.Position + 4 > reader.BaseStream.Length)
                    throw new EndOfStreamException("[Error] ReadLength: Expected 4 bytes but reached end of stream.");
                byte[] sizeBytes = reader.ReadBytes(4);
                return BitConverter.ToInt32(sizeBytes.Reverse().ToArray(), 0);
            case 0xC0:
                // The next object is encoded in a special format May be used to store numbers or strings
                Console.WriteLine("0xC0");
                return ParseSpecialEncoding(firstByte, reader);
            default:
                throw new InvalidDataException($"Invalid length encoding {firstByte}");
        }
    }

    private static int ParseSpecialEncoding(byte encodingType, BinaryReader reader)
    {
        switch (encodingType)
        {
            case 0xC0: // 8-bit integer
                return reader.ReadByte();
            case 0xC1: // 16-bit integer
                byte[] int16Bytes = new byte[2];
                reader.Read(int16Bytes, 0, 2);
                return BitConverter.ToInt16(int16Bytes, 0);
            case 0xC2: // 32-bit integer
                byte[] int32Bytes = new byte[4];
                reader.Read(int32Bytes, 0, 4);
                return BitConverter.ToInt32(int32Bytes, 0);
            default:
                throw new NotImplementedException("Unsupported special encoding type");
        }
    }

    private static int ReadLength(this BinaryReader reader, out bool isSpecialEncoding)
    {
        isSpecialEncoding = false;

        if (reader.BaseStream.Position >= reader.BaseStream.Length)
        {
            throw new EndOfStreamException("[Error] ReadLength: Reached end of stream unexpectedly.");
        }

        byte firstByte = reader.ReadByte();
        int length;

        switch ((firstByte & 0xC0) >> 6) // Extract top 2 bits
        {
            case 0b00: // 00xxxxxx -> The next 6 bits represent the length
                length = firstByte & 0x3F;
                break;

            case 0b01: // 01xxxxxx -> Read one additional byte (14-bit length)
                if (reader.BaseStream.Position >= reader.BaseStream.Length)
                {
                    throw new EndOfStreamException(
                        "[Error] ReadLength: Expected another byte but reached end of stream.");
                }

                length = ((firstByte & 0x3F) << 8) | reader.ReadByte();
                break;

            case 0b10: // 10xxxxxx -> Read the next 4 bytes as the length
                if (reader.BaseStream.Position + 4 > reader.BaseStream.Length)
                {
                    throw new EndOfStreamException("[Error] ReadLength: Expected 4 bytes but reached end of stream.");
                }

                length = reader.ReadInt32();
                break;

            case 0b11: // 11xxxxxx -> Special encoding (integer or LZF compression)
                isSpecialEncoding = true;
                length = firstByte & 0x3F;
                if (length > 3)
                {
                    throw new InvalidDataException(
                        $"[Error] ReadLength: Invalid special encoding {length}. Expected 0-3.");
                }

                break;

            default:
                throw new InvalidDataException($"[Error] ReadLength: Invalid length encoding {firstByte:X2}");
        }

        // Check if length is within valid range
        long remainingBytes = reader.BaseStream.Length - reader.BaseStream.Position;
        if (length < 0 || length > remainingBytes)
        {
            throw new InvalidDataException(
                $"[Error] ReadLength: Unrealistic length detected ({length} bytes requested, {remainingBytes} bytes available).");
        }

        return length;
    }

    private static object ReadString(BinaryReader reader)
    {
        var length = ReadLength(reader, out var isSpecialEncoding);
        Console.WriteLine($"[Debug] ReadString - Length: {length}, Special Encoding: {isSpecialEncoding}");

        long remainingBytes = reader.BaseStream.Length - reader.BaseStream.Position;
        if (length < 0 || length > remainingBytes)
        {
            throw new EndOfStreamException($"[Error] ReadString: Trying to read {length} bytes but only {remainingBytes} bytes left.");
        }

        if (!isSpecialEncoding)
        {
            byte[] data = reader.ReadBytes(length);
            Console.WriteLine($"[Debug] ReadString - Read {data.Length} bytes.");
            return Encoding.UTF8.GetString(data);
        }

        return length switch
        {
            0 => reader.ReadByte(),
            1 => reader.ReadInt16(),
            2 => reader.ReadInt32(),
            3 => ReadCompressedString(reader),
            _ => throw new InvalidDataException($"[Error] ReadString: Unknown special encoding {length}")
        };
    }


    private static string ReadCompressedString(BinaryReader reader)
    {
        Console.WriteLine($"[Debug] ReadCompressedString - Length: {reader.BaseStream.Length}");
        int compressedLength = reader.ReadLength(out _);
        int uncompressedLength = reader.ReadLength(out _);
        byte[] compressedData = reader.ReadBytes(compressedLength);

        using MemoryStream input = new(compressedData);
        using MemoryStream output = new(new byte[uncompressedLength], true);

        int result = LzfDecompressAlgorithm.Decompress(input, output, uncompressedLength);
        if (result <= 0)
            throw new InvalidDataException($"LZF Decompression failed");

        return Encoding.ASCII.GetString(output.ToArray());
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