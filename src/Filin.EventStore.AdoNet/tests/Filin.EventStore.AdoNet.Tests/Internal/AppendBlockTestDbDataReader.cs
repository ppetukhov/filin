#nullable disable
using System.Collections;
using System.Data.Common;

using Filin.EventStore.AdoNet.Internal;

namespace Filin.EventStore.AdoNet.Tests.Internal;

sealed class AppendBlockTestDbDataReader(List<(long Position, DateTime CreatedAt)> rows = null) : DbDataReader
{
    readonly List<(long Position, DateTime CreatedAt)> _rows = rows ?? [];
    int _currentIndex = -1;

    public override bool Read()
    {
        _currentIndex++;
        return _currentIndex < _rows.Count;
    }

    public override Task<bool> ReadAsync(CancellationToken cancellationToken) => Task.FromResult(Read());

    public override bool NextResult() => false;

    public override string GetName(int ordinal)
    {
        return ordinal switch
        {
            0 => ColumnNames.Position,
            1 => ColumnNames.CreatedAt,
            _ => string.Empty
        };
    }

    public override int GetOrdinal(string name)
    {
        return string.Equals(name, ColumnNames.Position, StringComparison.OrdinalIgnoreCase)
            ? 0
            : string.Equals(name, ColumnNames.CreatedAt, StringComparison.OrdinalIgnoreCase) ? 1 : 0;
    }

    public override long GetInt64(int ordinal) => _rows[_currentIndex].Position;
    public override DateTime GetDateTime(int ordinal) => _rows[_currentIndex].CreatedAt;
    public override int FieldCount => 2;
    public override bool HasRows => _rows.Count > 0;
    public override bool IsClosed => false;
    public override int Depth => 0;
    public override int RecordsAffected => _rows.Count;
    public override object GetValue(int ordinal) => ordinal == 0 ? GetInt64(0) : GetDateTime(1);
    public override object this[int ordinal] => GetValue(ordinal);
    public override object this[string name] => GetValue(GetOrdinal(name));
    public override bool GetBoolean(int ordinal) => false;
    public override byte GetByte(int ordinal) => 0;
    public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length) => 0;
    public override char GetChar(int ordinal) => '\0';
    public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length) => 0;
    public override string GetDataTypeName(int ordinal) => string.Empty;
    public override decimal GetDecimal(int ordinal) => 0;
    public override double GetDouble(int ordinal) => 0;
    public override Type GetFieldType(int ordinal) => typeof(object);
    public override float GetFloat(int ordinal) => 0;
    public override Guid GetGuid(int ordinal) => Guid.Empty;
    public override short GetInt16(int ordinal) => 0;
    public override int GetInt32(int ordinal) => 0;
    public override string GetString(int ordinal) => string.Empty;
    public override int GetValues(object[] values) => 0;
    public override bool IsDBNull(int ordinal) => false;
    public override IEnumerator GetEnumerator() => throw new NotImplementedException();
}

#nullable restore
