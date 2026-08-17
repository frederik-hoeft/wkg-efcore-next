using MySql.Data.MySqlClient;
using System.Data.Common;

namespace Wkg.EntityFrameworkCore.MySql.ProcedureMapping.Generation;

/// <summary>
/// Compiled MySQL result-column helpers invoked by source-generated execution plans.
/// </summary>
public static class MySqlColumnIntrinsics
{
    /// <summary>
    /// Creates an empty MySQL column configuration state.
    /// </summary>
    public static MySqlColumnState Create() => new();

    /// <summary>
    /// Records the provider-specific database type used for default reader selection.
    /// </summary>
    public static void HasDbType(ref MySqlColumnState state, MySqlDbType dbType) => state.DbType = dbType;

    /// <inheritdoc cref="GetAs(ref MySqlColumnState, MySqlColumnReaderKind)"/>
    public static void GetAsBoolean(ref MySqlColumnState state) => GetAs(ref state, MySqlColumnReaderKind.Boolean);
    /// <inheritdoc cref="GetAs(ref MySqlColumnState, MySqlColumnReaderKind)"/>
    public static void GetAsByte(ref MySqlColumnState state) => GetAs(ref state, MySqlColumnReaderKind.Byte);
    /// <inheritdoc cref="GetAs(ref MySqlColumnState, MySqlColumnReaderKind)"/>
    public static void GetAsBytes(ref MySqlColumnState state) => GetAs(ref state, MySqlColumnReaderKind.Bytes);
    /// <inheritdoc cref="GetAs(ref MySqlColumnState, MySqlColumnReaderKind)"/>
    public static void GetAsDateTime(ref MySqlColumnState state) => GetAs(ref state, MySqlColumnReaderKind.DateTime);
    /// <inheritdoc cref="GetAs(ref MySqlColumnState, MySqlColumnReaderKind)"/>
    public static void GetAsDecimal(ref MySqlColumnState state) => GetAs(ref state, MySqlColumnReaderKind.Decimal);
    /// <inheritdoc cref="GetAs(ref MySqlColumnState, MySqlColumnReaderKind)"/>
    public static void GetAsDouble(ref MySqlColumnState state) => GetAs(ref state, MySqlColumnReaderKind.Double);
    /// <inheritdoc cref="GetAs(ref MySqlColumnState, MySqlColumnReaderKind)"/>
    public static void GetAsFloat(ref MySqlColumnState state) => GetAs(ref state, MySqlColumnReaderKind.Float);
    /// <inheritdoc cref="GetAs(ref MySqlColumnState, MySqlColumnReaderKind)"/>
    public static void GetAsGuid(ref MySqlColumnState state) => GetAs(ref state, MySqlColumnReaderKind.Guid);
    /// <inheritdoc cref="GetAs(ref MySqlColumnState, MySqlColumnReaderKind)"/>
    public static void GetAsInt16(ref MySqlColumnState state) => GetAs(ref state, MySqlColumnReaderKind.Int16);
    /// <inheritdoc cref="GetAs(ref MySqlColumnState, MySqlColumnReaderKind)"/>
    public static void GetAsInt32(ref MySqlColumnState state) => GetAs(ref state, MySqlColumnReaderKind.Int32);
    /// <inheritdoc cref="GetAs(ref MySqlColumnState, MySqlColumnReaderKind)"/>
    public static void GetAsInt64(ref MySqlColumnState state) => GetAs(ref state, MySqlColumnReaderKind.Int64);
    /// <inheritdoc cref="GetAs(ref MySqlColumnState, MySqlColumnReaderKind)"/>
    public static void GetAsSByte(ref MySqlColumnState state) => GetAs(ref state, MySqlColumnReaderKind.SByte);
    /// <inheritdoc cref="GetAs(ref MySqlColumnState, MySqlColumnReaderKind)"/>
    public static void GetAsString(ref MySqlColumnState state) => GetAs(ref state, MySqlColumnReaderKind.String);
    /// <inheritdoc cref="GetAs(ref MySqlColumnState, MySqlColumnReaderKind)"/>
    public static void GetAsTimeSpan(ref MySqlColumnState state) => GetAs(ref state, MySqlColumnReaderKind.TimeSpan);
    /// <inheritdoc cref="GetAs(ref MySqlColumnState, MySqlColumnReaderKind)"/>
    public static void GetAsUInt16(ref MySqlColumnState state) => GetAs(ref state, MySqlColumnReaderKind.UInt16);
    /// <inheritdoc cref="GetAs(ref MySqlColumnState, MySqlColumnReaderKind)"/>
    public static void GetAsUInt32(ref MySqlColumnState state) => GetAs(ref state, MySqlColumnReaderKind.UInt32);
    /// <inheritdoc cref="GetAs(ref MySqlColumnState, MySqlColumnReaderKind)"/>
    public static void GetAsUInt64(ref MySqlColumnState state) => GetAs(ref state, MySqlColumnReaderKind.UInt64);
    /// <inheritdoc cref="GetAs(ref MySqlColumnState, MySqlColumnReaderKind)"/>
    public static void GetAsJson(ref MySqlColumnState state) => GetAs(ref state, MySqlColumnReaderKind.String);
    /// <inheritdoc cref="GetAs(ref MySqlColumnState, MySqlColumnReaderKind)"/>
    public static void GetAsStream(ref MySqlColumnState state) => GetAs(ref state, MySqlColumnReaderKind.Stream);
    /// <inheritdoc cref="GetAs(ref MySqlColumnState, MySqlColumnReaderKind)"/>
    public static void GetAsMySqlDateTime(ref MySqlColumnState state) => GetAs(ref state, MySqlColumnReaderKind.MySqlDateTime);
    /// <inheritdoc cref="GetAs(ref MySqlColumnState, MySqlColumnReaderKind)"/>
    public static void GetAsMySqlDecimal(ref MySqlColumnState state) => GetAs(ref state, MySqlColumnReaderKind.MySqlDecimal);
    /// <inheritdoc cref="GetAs(ref MySqlColumnState, MySqlColumnReaderKind)"/>
    public static void GetAsMySqlGeometry(ref MySqlColumnState state) => GetAs(ref state, MySqlColumnReaderKind.MySqlGeometry);

    /// <summary>
    /// Selects an explicit reader kind.
    /// </summary>
    public static void GetAs(ref MySqlColumnState state, MySqlColumnReaderKind kind) => state.ReaderKind = kind;

    /// <summary>
    /// Reads the configured column from <paramref name="reader"/>.
    /// </summary>
    public static object? Read(ref MySqlColumnState state, DbDataReader reader, string columnName, bool isNullable)
    {
        int ordinal = reader.GetOrdinal(columnName);
        if (isNullable && reader.IsDBNull(ordinal))
        {
            return null;
        }

        return (state.ReaderKind ?? InferReaderKind(state.DbType)) switch
        {
            MySqlColumnReaderKind.Boolean => reader.GetBoolean(ordinal),
            MySqlColumnReaderKind.Byte => reader.GetByte(ordinal),
            MySqlColumnReaderKind.Bytes => ReadBytes(reader, ordinal),
            MySqlColumnReaderKind.DateTime => reader.GetDateTime(ordinal),
            MySqlColumnReaderKind.Decimal => reader.GetDecimal(ordinal),
            MySqlColumnReaderKind.Double => reader.GetDouble(ordinal),
            MySqlColumnReaderKind.Float => reader.GetFloat(ordinal),
            MySqlColumnReaderKind.Guid => reader.GetGuid(ordinal),
            MySqlColumnReaderKind.Int16 => reader.GetInt16(ordinal),
            MySqlColumnReaderKind.Int32 => reader.GetInt32(ordinal),
            MySqlColumnReaderKind.Int64 => reader.GetInt64(ordinal),
            MySqlColumnReaderKind.SByte => reader.GetFieldValue<sbyte>(ordinal),
            MySqlColumnReaderKind.String => reader.GetString(ordinal),
            MySqlColumnReaderKind.TimeSpan => reader.GetFieldValue<TimeSpan>(ordinal),
            MySqlColumnReaderKind.UInt16 => reader.GetFieldValue<ushort>(ordinal),
            MySqlColumnReaderKind.UInt32 => reader.GetFieldValue<uint>(ordinal),
            MySqlColumnReaderKind.UInt64 => reader.GetFieldValue<ulong>(ordinal),
            MySqlColumnReaderKind.Stream => reader.GetStream(ordinal),
            MySqlColumnReaderKind.MySqlDateTime when reader is MySqlDataReader mySql => mySql.GetMySqlDateTime(ordinal),
            MySqlColumnReaderKind.MySqlDecimal when reader is MySqlDataReader mySql => mySql.GetMySqlDecimal(ordinal),
            MySqlColumnReaderKind.MySqlGeometry when reader is MySqlDataReader mySql => mySql.GetMySqlGeometry(ordinal),
            _ => reader.GetValue(ordinal)
        };
    }

    private static byte[] ReadBytes(DbDataReader reader, int ordinal)
    {
        long length = reader.GetBytes(ordinal, 0, null, 0, 0);
        byte[] buffer = new byte[length];
        _ = reader.GetBytes(ordinal, 0, buffer, 0, buffer.Length);
        return buffer;
    }

    private static MySqlColumnReaderKind InferReaderKind(MySqlDbType? dbType) => dbType switch
    {
        MySqlDbType.Int32 or MySqlDbType.Int24 => MySqlColumnReaderKind.Int32,
        MySqlDbType.Int64 => MySqlColumnReaderKind.Int64,
        MySqlDbType.Int16 => MySqlColumnReaderKind.Int16,
        MySqlDbType.UInt32 or MySqlDbType.UInt24 => MySqlColumnReaderKind.UInt32,
        MySqlDbType.UInt64 => MySqlColumnReaderKind.UInt64,
        MySqlDbType.UInt16 => MySqlColumnReaderKind.UInt16,
        MySqlDbType.Byte => MySqlColumnReaderKind.SByte,
        MySqlDbType.UByte => MySqlColumnReaderKind.Byte,
        MySqlDbType.Bit => MySqlColumnReaderKind.Boolean,
        MySqlDbType.VarChar or MySqlDbType.String or MySqlDbType.Text or MySqlDbType.TinyText
            or MySqlDbType.MediumText or MySqlDbType.LongText or MySqlDbType.VarString or MySqlDbType.JSON => MySqlColumnReaderKind.String,
        MySqlDbType.Date or MySqlDbType.DateTime or MySqlDbType.Timestamp => MySqlColumnReaderKind.DateTime,
        MySqlDbType.Decimal or MySqlDbType.NewDecimal => MySqlColumnReaderKind.Decimal,
        MySqlDbType.Double => MySqlColumnReaderKind.Double,
        MySqlDbType.Float => MySqlColumnReaderKind.Float,
        MySqlDbType.Guid => MySqlColumnReaderKind.Guid,
        MySqlDbType.Time => MySqlColumnReaderKind.TimeSpan,
        MySqlDbType.Binary or MySqlDbType.VarBinary or MySqlDbType.Blob or MySqlDbType.TinyBlob
            or MySqlDbType.MediumBlob or MySqlDbType.LongBlob => MySqlColumnReaderKind.Bytes,
        _ => MySqlColumnReaderKind.Default
    };
}

/// <summary>
/// Opaque MySQL column configuration state.
/// </summary>
public struct MySqlColumnState
{
    /// <summary>
    /// The configured database type, if any.
    /// </summary>
    public MySqlDbType? DbType { get; set; }

    /// <summary>
    /// The explicit reader kind selected by a <c>GetAs*</c> terminal.
    /// </summary>
    public MySqlColumnReaderKind? ReaderKind { get; set; }
}

/// <summary>
/// Explicit MySQL column reader kinds selected by generated plans.
/// </summary>
#pragma warning disable CS1591
public enum MySqlColumnReaderKind
{
    Default = 0,
    Boolean,
    Byte,
    Bytes,
    DateTime,
    Decimal,
    Double,
    Float,
    Guid,
    Int16,
    Int32,
    Int64,
    SByte,
    String,
    TimeSpan,
    UInt16,
    UInt32,
    UInt64,
    Stream,
    MySqlDateTime,
    MySqlDecimal,
    MySqlGeometry
}
#pragma warning restore CS1591
