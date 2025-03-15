using System;
using System.Collections.Generic;
using System.Reflection.Metadata;

namespace codecrafters_redis.RedisRepositories.Storage;

/// <summary>
/// Represents a full Redis RDB file with multiple databases, auxiliary fields, and checksum validation.
/// </summary>
internal class RdbFile
{
    public int Version { get; private set; }
    public Dictionary<int, RdbDatabase> Databases { get; private set; } = new Dictionary<int, RdbDatabase>();
    public Dictionary<string, string> AuxiliaryFields { get; private set; } = new Dictionary<string, string>();
    public byte[] Checksum { get; private set; } = new byte[8];
    public bool IsChecksumValid { get; private set; } = false;

    private RdbFile() { }

    public class RdbFileBuilder
    {
        private readonly RdbFile _rdbFile = new RdbFile();

        public RdbFileBuilder WithVersion(int version)
        {
            _rdbFile.Version = version;
            return this;
        }

        public RdbFileBuilder AddDatabase(RdbDatabase database)
        {
            _rdbFile.Databases[database.DatabaseIndex] = database;
            return this;
        }

        public RdbFileBuilder AddAuxiliaryField(string key, string value)
        {
            _rdbFile.AuxiliaryFields[key] = value;
            return this;
        }

        public RdbFileBuilder WithChecksum(byte[] checksum, bool isValid)
        {
            _rdbFile.Checksum = checksum;
            _rdbFile.IsChecksumValid = isValid;
            return this;
        }

        public bool ContainsDatabase(int dbIndex) => _rdbFile.Databases.ContainsKey(dbIndex);

        public RdbFileBuilder AddEntryToDatabase(int dbIndex, RdbEntry entry)
        {
            if (!_rdbFile.Databases.ContainsKey(dbIndex))
            {
                _rdbFile.Databases[dbIndex] = new RdbDatabase(dbIndex, new());
            }

            _rdbFile.Databases[dbIndex].AddEntry(entry);
            return this;
        }

        public RdbFile Build() => _rdbFile;
    }
}
/// <summary>
/// Represents a single Redis database inside an RDB file.
/// </summary>
internal class RdbDatabase
{
    public int DatabaseIndex { get; private set; }
    public Dictionary<string, RdbEntry> KeyValues { get; private set; } = new Dictionary<string, RdbEntry>();

    public RdbDatabase(int databaseIndex, Dictionary<string, RdbEntry> keyValues)
    {
        DatabaseIndex = databaseIndex;
        KeyValues = keyValues;
    }

    private RdbDatabase() { }

    public void AddEntry(RdbEntry entry)
    {
        KeyValues[entry.Key] = entry;
    }

    public class RdbDatabaseBuilder()
    {
        private readonly RdbDatabase _rdbDatabase = new RdbDatabase();

        public RdbDatabaseBuilder WithDatabaseIndex(int index)
        {
            _rdbDatabase.DatabaseIndex = index;
            return this;
        }

        public RdbDatabaseBuilder AddEntry(RdbEntry entry)
        {
            _rdbDatabase.KeyValues[entry.Key] = entry;
            return this;
        }

        public RdbDatabase Build() => _rdbDatabase;
    }
}
/// <summary>
/// Represents a single key-value entry in an RDB file, including expiration time and type.
/// </summary>
internal class RdbEntry
{
    public string Key { get; private set; } = string.Empty;
    public byte[] Value { get; private set; } = [];
    public DateTime Expiry { get; private set; } = DateTime.MaxValue;
    public RdbValueType Type { get; private set; } = RdbValueType.String;

    public RdbEntry(string key, byte[] value, DateTime expiry, RdbValueType type)
    {
        Key = key;
        Value = value;
        Expiry = expiry;
        Type = type;
    }

    private RdbEntry()
    {
        
    }

    public class RdbEntryBuilder
    {
       private readonly RdbEntry _rdbEntry = new RdbEntry();

        public RdbEntryBuilder WithKey(string key)
        {
            _rdbEntry.Key = key;
            return this;
        }

        public RdbEntryBuilder WithValue(byte[] value)
        {
            _rdbEntry.Value = value;
            return this;
        }

        public RdbEntryBuilder WithExpiry(DateTime? expiry)
        {
            _rdbEntry.Expiry = expiry ?? DateTime.MaxValue;
            return this;
        }

        public RdbEntryBuilder WithType(RdbValueType type)
        {
            _rdbEntry.Type = type;
            return this;
        }

        public RdbEntry Build() => _rdbEntry;
    }
}

/// <summary>
/// Enum representing different Redis data types stored in RDB files.
/// </summary>
internal enum RdbValueType
{
    String = 0,
    List = 1,
    Set = 2,
    SortedSet = 3,
    Hash = 4,
    Stream = 5
}

