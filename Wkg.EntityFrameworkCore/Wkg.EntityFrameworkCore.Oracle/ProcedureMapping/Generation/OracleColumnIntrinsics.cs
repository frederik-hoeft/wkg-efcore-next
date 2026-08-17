using Oracle.ManagedDataAccess.Client;
using System.Data.Common;

namespace Wkg.EntityFrameworkCore.Oracle.ProcedureMapping.Generation;

/// <summary>
/// Compiled Oracle result-column helpers invoked by source-generated execution plans.
/// </summary>
public static class OracleColumnIntrinsics
{
    /// <summary>
    /// Creates an empty Oracle column configuration state.
    /// </summary>
    public static OracleColumnState Create() => new();

    /// <summary>
    /// Records the provider-specific database type used for default reader selection.
    /// </summary>
    public static void HasDbType(ref OracleColumnState state, OracleDbType dbType) => state.DbType = dbType;

    /// <inheritdoc cref="GetAs(ref OracleColumnState, OracleColumnReaderKind)"/>
    public static void GetAsBoolean(ref OracleColumnState state) => GetAs(ref state, OracleColumnReaderKind.Boolean);
    /// <inheritdoc cref="GetAs(ref OracleColumnState, OracleColumnReaderKind)"/>
    public static void GetAsByte(ref OracleColumnState state) => GetAs(ref state, OracleColumnReaderKind.Byte);
    /// <inheritdoc cref="GetAs(ref OracleColumnState, OracleColumnReaderKind)"/>
    public static void GetAsBytes(ref OracleColumnState state) => GetAs(ref state, OracleColumnReaderKind.Bytes);
    /// <inheritdoc cref="GetAs(ref OracleColumnState, OracleColumnReaderKind)"/>
    public static void GetAsChar(ref OracleColumnState state) => GetAs(ref state, OracleColumnReaderKind.Char);
    /// <inheritdoc cref="GetAs(ref OracleColumnState, OracleColumnReaderKind)"/>
    public static void GetAsDateTime(ref OracleColumnState state) => GetAs(ref state, OracleColumnReaderKind.DateTime);
    /// <inheritdoc cref="GetAs(ref OracleColumnState, OracleColumnReaderKind)"/>
    public static void GetAsDateTimeOffset(ref OracleColumnState state) => GetAs(ref state, OracleColumnReaderKind.DateTimeOffset);
    /// <inheritdoc cref="GetAs(ref OracleColumnState, OracleColumnReaderKind)"/>
    public static void GetAsDecimal(ref OracleColumnState state) => GetAs(ref state, OracleColumnReaderKind.Decimal);
    /// <inheritdoc cref="GetAs(ref OracleColumnState, OracleColumnReaderKind)"/>
    public static void GetAsDouble(ref OracleColumnState state) => GetAs(ref state, OracleColumnReaderKind.Double);
    /// <inheritdoc cref="GetAs(ref OracleColumnState, OracleColumnReaderKind)"/>
    public static void GetAsFloat(ref OracleColumnState state) => GetAs(ref state, OracleColumnReaderKind.Float);
    /// <inheritdoc cref="GetAs(ref OracleColumnState, OracleColumnReaderKind)"/>
    public static void GetAsGuid(ref OracleColumnState state) => GetAs(ref state, OracleColumnReaderKind.Guid);
    /// <inheritdoc cref="GetAs(ref OracleColumnState, OracleColumnReaderKind)"/>
    public static void GetAsInt16(ref OracleColumnState state) => GetAs(ref state, OracleColumnReaderKind.Int16);
    /// <inheritdoc cref="GetAs(ref OracleColumnState, OracleColumnReaderKind)"/>
    public static void GetAsInt32(ref OracleColumnState state) => GetAs(ref state, OracleColumnReaderKind.Int32);
    /// <inheritdoc cref="GetAs(ref OracleColumnState, OracleColumnReaderKind)"/>
    public static void GetAsInt64(ref OracleColumnState state) => GetAs(ref state, OracleColumnReaderKind.Int64);
    /// <inheritdoc cref="GetAs(ref OracleColumnState, OracleColumnReaderKind)"/>
    public static void GetAsString(ref OracleColumnState state) => GetAs(ref state, OracleColumnReaderKind.String);
    /// <inheritdoc cref="GetAs(ref OracleColumnState, OracleColumnReaderKind)"/>
    public static void GetAsTimeSpan(ref OracleColumnState state) => GetAs(ref state, OracleColumnReaderKind.TimeSpan);
    /// <inheritdoc cref="GetAs(ref OracleColumnState, OracleColumnReaderKind)"/>
    public static void GetAsJson(ref OracleColumnState state) => GetAs(ref state, OracleColumnReaderKind.String);

    /// <summary>
    /// Selects an explicit reader kind.
    /// </summary>
    public static void GetAs(ref OracleColumnState state, OracleColumnReaderKind kind) => state.ReaderKind = kind;

    /// <summary>
    /// Reads the configured column from <paramref name="reader"/>.
    /// </summary>
    public static object? Read(ref OracleColumnState state, DbDataReader reader, string columnName, bool isNullable)
    {
        int ordinal = reader.GetOrdinal(columnName);
        if (isNullable && reader.IsDBNull(ordinal))
        {
            return null;
        }

        return (state.ReaderKind ?? InferReaderKind(state.DbType)) switch
        {
            OracleColumnReaderKind.Boolean => reader.GetBoolean(ordinal),
            OracleColumnReaderKind.Byte => reader.GetByte(ordinal),
            OracleColumnReaderKind.Bytes => ReadBytes(reader, ordinal),
            OracleColumnReaderKind.Char => reader.GetChar(ordinal),
            OracleColumnReaderKind.DateTime => reader.GetDateTime(ordinal),
            OracleColumnReaderKind.DateTimeOffset => reader.GetFieldValue<DateTimeOffset>(ordinal),
            OracleColumnReaderKind.Decimal => reader.GetDecimal(ordinal),
            OracleColumnReaderKind.Double => reader.GetDouble(ordinal),
            OracleColumnReaderKind.Float => reader.GetFloat(ordinal),
            OracleColumnReaderKind.Guid => reader.GetGuid(ordinal),
            OracleColumnReaderKind.Int16 => reader.GetInt16(ordinal),
            OracleColumnReaderKind.Int32 => reader.GetInt32(ordinal),
            OracleColumnReaderKind.Int64 => reader.GetInt64(ordinal),
            OracleColumnReaderKind.String => reader.GetString(ordinal),
            OracleColumnReaderKind.TimeSpan => reader.GetFieldValue<TimeSpan>(ordinal),
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

    private static OracleColumnReaderKind InferReaderKind(OracleDbType? dbType) => dbType switch
    {
        OracleDbType.Int32 => OracleColumnReaderKind.Int32,
        OracleDbType.Int64 => OracleColumnReaderKind.Int64,
        OracleDbType.Int16 => OracleColumnReaderKind.Int16,
        OracleDbType.Byte => OracleColumnReaderKind.Byte,
        OracleDbType.Boolean => OracleColumnReaderKind.Boolean,
        OracleDbType.Varchar2 or OracleDbType.NVarchar2 or OracleDbType.Char or OracleDbType.NChar
            or OracleDbType.Clob or OracleDbType.NClob or OracleDbType.Json => OracleColumnReaderKind.String,
        OracleDbType.Date or OracleDbType.TimeStamp => OracleColumnReaderKind.DateTime,
        OracleDbType.TimeStampTZ or OracleDbType.TimeStampLTZ => OracleColumnReaderKind.DateTimeOffset,
        OracleDbType.Decimal => OracleColumnReaderKind.Decimal,
        OracleDbType.Double => OracleColumnReaderKind.Double,
        OracleDbType.Single => OracleColumnReaderKind.Float,
        OracleDbType.Raw or OracleDbType.Blob or OracleDbType.LongRaw => OracleColumnReaderKind.Bytes,
        OracleDbType.IntervalDS => OracleColumnReaderKind.TimeSpan,
        _ => OracleColumnReaderKind.Default
    };
}

/// <summary>
/// Opaque Oracle column configuration state.
/// </summary>
public struct OracleColumnState
{
    /// <summary>
    /// The configured database type, if any.
    /// </summary>
    public OracleDbType? DbType { get; set; }

    /// <summary>
    /// The explicit reader kind selected by a <c>GetAs*</c> terminal.
    /// </summary>
    public OracleColumnReaderKind? ReaderKind { get; set; }
}

/// <summary>
/// Explicit Oracle column reader kinds selected by generated plans.
/// </summary>
#pragma warning disable CS1591
public enum OracleColumnReaderKind
{
    Default = 0,
    Boolean,
    Byte,
    Bytes,
    Char,
    DateTime,
    DateTimeOffset,
    Decimal,
    Double,
    Float,
    Guid,
    Int16,
    Int32,
    Int64,
    String,
    TimeSpan
}
#pragma warning restore CS1591
