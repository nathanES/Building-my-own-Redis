using System.Text;

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
        using BinaryReader reader = new(fileStream, Encoding.UTF8, leaveOpen: true);
        if (!ValidateHeader(reader))
        {
            Console.WriteLine("Invalid Header");
            return null;
        }

        var rdbFileBuilder = new RdbFile.RdbFileBuilder();
        var databaseBuilders = new Dictionary<int, RdbDatabase.RdbDatabaseBuilder>();
        RdbDatabase.RdbDatabaseBuilder? currentDbBuilder = null;
        int currentDb = 0;
        bool isEof = false;
        DateTime? currentExpiration = null;

        while (reader.BaseStream.Position < reader.BaseStream.Length - 8 && !isEof) // Exclude checksum
        {
            byte currentByte = reader.ReadByte();

            switch (currentByte)
            {
                case DatabaseSelector: 
                    currentDb = reader.ReadByte();
                    Console.WriteLine($"Switching to DB {currentDb}");

                    if (!databaseBuilders.TryGetValue(currentDb, out currentDbBuilder))
                    {
                        currentDbBuilder = new RdbDatabase.RdbDatabaseBuilder().WithDatabaseIndex(currentDb);
                        databaseBuilders[currentDb] = currentDbBuilder;
                    }
                    break;

                case ExpireTime: 
                    int expireSeconds = reader.ReadInt32();
                    currentExpiration = DateTime.UtcNow.AddSeconds(expireSeconds);
                    Console.WriteLine($"Expiration (seconds): {expireSeconds}");
                    break;

                case ExpireTimeMs: 
                    long expireMs = reader.ReadInt64();
                    currentExpiration = DateTime.UtcNow.AddMilliseconds(expireMs);
                    Console.WriteLine($"Expiration (milliseconds): {expireMs}");
                    break;

                case ResizeDb: 
                    int hashTableSize = reader.ReadInt32();
                    int expiryTableSize = reader.ReadInt32();
                    Console.WriteLine($"Resizing DB: hashTableSize={hashTableSize}, expiryTableSize={expiryTableSize}");
                    break;

                case Auxiliary: 
                    string auxKey = ReadString(reader);
                    string auxValue = ReadString(reader);
                    rdbFileBuilder.AddAuxiliaryField(auxKey, auxValue);
                    Console.WriteLine($"Auxiliary field: {auxKey} = {auxValue}");
                    break;
                
                case EndOfFile:
                    isEof = true;
                    break;

                default:
                    string key = ReadString(reader);
                    byte[] value = ReadBytes(reader);

                    if (currentDbBuilder == null)
                    {
                        currentDbBuilder = new RdbDatabase.RdbDatabaseBuilder().WithDatabaseIndex(currentDb);
                        databaseBuilders[currentDb] = currentDbBuilder;
                    }
                    
                    var entry = new RdbEntry.RdbEntryBuilder()
                        .WithKey(key)
                        .WithValue(value)
                        .WithExpiry(currentExpiration)
                        .Build();

                    currentDbBuilder!.AddEntry(entry);
                    Console.WriteLine($"Read Key-Value in DB {currentDb}: {key}");

                    currentExpiration = null;
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
    private static bool ValidateHeader(BinaryReader reader)
    {
        byte[] header = reader.ReadBytes(9);
        return Encoding.ASCII.GetString(header) == "REDIS0011";
    }
    private static string ReadString(BinaryReader reader)
    {
        int length = reader.ReadByte();
        byte[] buffer = reader.ReadBytes(length);
        return Encoding.UTF8.GetString(buffer);
    }

    private static byte[] ReadBytes(BinaryReader reader)
    {
        int length = reader.ReadByte(); 
        return reader.ReadBytes(length);
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