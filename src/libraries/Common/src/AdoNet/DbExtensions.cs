using System.Buffers;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics.CodeAnalysis;

using Filin.Storage;

namespace Filin.AdoNet;

/// <summary>
/// Contains some convenience methods to use with IRelationalStorage.
/// </summary>
static class DbExtensions
{
    /// <summary>
    /// An explicit map of type CLR viz database type conversions.
    /// </summary>
    static readonly ReadOnlyDictionary<Type, DbType> TypeMap = new(new Dictionary<Type, DbType>
    {
        { typeof(object),   DbType.Object },
        { typeof(int),      DbType.Int32 },
        { typeof(int?),     DbType.Int32 },
        { typeof(uint),     DbType.UInt32 },
        { typeof(uint?),    DbType.UInt32 },
        { typeof(long),     DbType.Int64 },
        { typeof(long?),    DbType.Int64 },
        { typeof(ulong),    DbType.UInt64 },
        { typeof(ulong?),   DbType.UInt64 },
        { typeof(float),    DbType.Single },
        { typeof(float?),   DbType.Single },
        { typeof(double),   DbType.Double },
        { typeof(double?),  DbType.Double },
        { typeof(decimal),  DbType.Decimal },
        { typeof(decimal?), DbType.Decimal },
        { typeof(short),    DbType.Int16 },
        { typeof(short?),   DbType.Int16 },
        { typeof(ushort),   DbType.UInt16 },
        { typeof(ushort?),  DbType.UInt16 },
        { typeof(byte),     DbType.Byte },
        { typeof(byte?),    DbType.Byte },
        { typeof(sbyte),    DbType.SByte },
        { typeof(sbyte?),   DbType.SByte },
        { typeof(bool),     DbType.Boolean },
        { typeof(bool?),    DbType.Boolean },
        { typeof(string),   DbType.String },
        { typeof(char),     DbType.StringFixedLength },
        { typeof(char?),    DbType.StringFixedLength },
        { typeof(Guid),     DbType.Guid },
        { typeof(Guid?),    DbType.Guid },
        //Using DateTime for cross DB compatibility. The underlying DB table column type can be DateTime or DateTime2
        { typeof(DateTime),     DbType.DateTime },
        { typeof(DateTime?),    DbType.DateTime },
        { typeof(TimeSpan),     DbType.Time },
        { typeof(byte[]),       DbType.Binary },
        { typeof(TimeSpan?),        DbType.Time },
        { typeof(DateTimeOffset),   DbType.DateTimeOffset },
        { typeof(DateTimeOffset?),  DbType.DateTimeOffset },
    });

    /// <summary>
    /// Creates a new SQL parameter using the given arguments.
    /// </summary>
    /// <typeparam name="T">The type of the parameter.</typeparam>
    /// <param name="command">The command to use to create the parameter.</param>
    /// <param name="direction">The direction of the parameter.</param>
    /// <param name="value">The value of the parameter.</param>
    /// <param name="size">The size of the parameter value.</param>
    /// <param name="dbType">the <see cref="DbType"/> of the parameter.</param>
    /// <returns>A parameter created using the given arguments.</returns>
    public static IDbDataParameter CreateParameter<T>(this IDbCommand command, ParameterDirection direction, T value, int? size = null, DbType? dbType = null)
    {
        //There should be no boxing for value types. See at:
        //http://stackoverflow.com/questions/8823239/comparing-a-generic-against-null-that-could-be-a-value-or-reference-type
        var parameter = command.CreateParameter();
        parameter.Value = (object?)value ?? DBNull.Value;
        parameter.DbType = dbType ?? TypeMap[typeof(T)];
        parameter.Direction = direction;
        if (size != null) { parameter.Size = size.Value; }

        return parameter;
    }

    /// <summary>
    /// Creates a new SQL parameter using the given arguments.
    /// </summary>
    /// <typeparam name="T">The type of the parameter.</typeparam>
    /// <param name="command">The command to use to create the parameter.</param>
    /// <param name="direction">The direction of the parameter.</param>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <param name="value">The value of the parameter.</param>
    /// <param name="size">The size of the parameter value.</param>
    /// <param name="dbType">the <see cref="DbType"/> of the parameter.</param>
    /// <returns>A parameter created using the given arguments.</returns>
    public static IDbDataParameter CreateParameter<T>(this IDbCommand command, ParameterDirection direction, string parameterName, T value, int? size = null, DbType? dbType = null)
    {
        //There should be no boxing for value types. See at:
        //http://stackoverflow.com/questions/8823239/comparing-a-generic-against-null-that-could-be-a-value-or-reference-type
        var parameter = command.CreateParameter();
        parameter.ParameterName = parameterName;
        parameter.Value = (object?)value ?? DBNull.Value;
        parameter.DbType = dbType ?? TypeMap[typeof(T)];
        parameter.Direction = direction;
        if (size != null) { parameter.Size = size.Value; }

        return parameter;
    }

    /// <summary>
    /// Creates and adds a new SQL parameter to the command.
    /// </summary>
    /// <typeparam name="T">The type of the parameter.</typeparam>
    /// <param name="command">The command to use to create the parameter.</param>
    /// <param name="value">The value of the parameter.</param>
    /// <param name="direction">The direction of the parameter.</param>
    /// <param name="size">The size of the parameter value.</param>
    /// <param name="dbType">the <see cref="DbType"/> of the parameter.</param>
    public static void AddParameter<T>(this IDbCommand command, T value, ParameterDirection direction = ParameterDirection.Input, int? size = null, DbType? dbType = null) => command.Parameters.Add(command.CreateParameter(direction, value, size, dbType));

    /// <summary>
    /// Creates and adds a new SQL parameter to the command.
    /// </summary>
    /// <typeparam name="T">The type of the parameter.</typeparam>
    /// <param name="command">The command to use to create the parameter.</param>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <param name="value">The value of the parameter.</param>
    /// <param name="direction">The direction of the parameter.</param>
    /// <param name="size">The size of the parameter value.</param>
    /// <param name="dbType">the <see cref="DbType"/> of the parameter.</param>
    public static void AddParameter<T>(this IDbCommand command, string parameterName, T value, ParameterDirection direction = ParameterDirection.Input, int? size = null, DbType? dbType = null) => command.Parameters.Add(command.CreateParameter(direction, parameterName, value, size, dbType));

    /// <summary>
    /// Returns a value with the given <see paramref="fieldName"/>.
    /// </summary>
    /// <typeparam name="TValue">The type of value to retrieve.</typeparam>
    /// <param name="record">The record from which to retrieve the value.</param>
    /// <param name="fieldName">The name of the field.</param>
    /// <returns>Suffix in the given field indicated by <see paramref="fieldName"/>.</returns>
    /// <exception cref="DataException"/>
    /// <remarks>This function throws if the given <see paramref="fieldName"/> does not exist.</remarks>        
    public static TValue GetValue<TValue>(this IDataRecord record, string fieldName)
    {
        try
        {
            var ordinal = record.GetOrdinal(fieldName);
            return (TValue)record.GetValue(ordinal);
        }
        catch (IndexOutOfRangeException e)
        {
            throw new DataException($"Field '{fieldName}' not found in data record.", e);
        }
    }

    internal static bool TryGetPayload(this IDataRecord record, int ordinal, Type payloadType, IStorageSerializer serializer, [NotNullWhen(true)] out object? payload)
    {
        const int CHUNK_SIZE = 4096;

        // https://learn.microsoft.com/en-us/dotnet/api/system.data.idatarecord.getbytes?view=net-10.0#remarks
        // If you pass a buffer that is null, GetBytes returns the length of the row in bytes.
        var payloadBytesCount = (int)record.GetBytes(i: ordinal, fieldOffset: 0, buffer: null, bufferoffset: 0, length: 0);

        if (payloadBytesCount == 0)
        {
            payload = null;
            return false;
        }

        var buf = ArrayPool<byte>.Shared.Rent(payloadBytesCount);

        try
        {
            var readBufferSize = Math.Min(CHUNK_SIZE, payloadBytesCount);
            long bytesRead;
            long offset = 0;

            do
            {
                bytesRead = record.GetBytes(ordinal, offset, buf, (int)offset, readBufferSize);
                offset += bytesRead;
            }
            while (bytesRead > 0 && offset < payloadBytesCount);

            var dataSpan = buf.AsSpan()[..payloadBytesCount];

            payload = serializer.Deserialize(payloadType, dataSpan);
            return true;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buf);
        }
    }
}
