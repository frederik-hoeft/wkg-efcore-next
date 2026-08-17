using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Data.Common;

namespace Wkg.EntityFrameworkCore.Oracle.ProcedureMapping.Generation;

/// <summary>
/// Compiled Oracle parameter helpers invoked by source-generated execution plans.
/// </summary>
public static class OracleParameterIntrinsics
{
    /// <summary>
    /// Creates an empty Oracle parameter configuration state.
    /// </summary>
    public static OracleParameterState Create() => new();

    /// <summary>
    /// Records the provider-specific database type.
    /// </summary>
    public static void HasDbType(ref OracleParameterState state, OracleDbType dbType) => state.DbType = dbType;

    /// <summary>
    /// Materializes an <see cref="OracleParameter"/> from the accumulated configuration and runtime value.
    /// </summary>
    public static DbParameter Finalize(ref OracleParameterState state, string name, ParameterDirection direction, int size, Type clrType, object? value)
    {
        OracleDbType dbType = state.DbType ?? InferDbType(clrType);
        return new OracleParameter(name, dbType, value ?? DBNull.Value, direction)
        {
            Size = size
        };
    }

    /// <summary>
    /// Reads an output parameter value.
    /// </summary>
    public static T Store<T>(DbParameter parameter)
    {
        object? value = parameter.Value;
        if (value is null or DBNull)
        {
            return default!;
        }

        if (value is T typed)
        {
            return typed;
        }

        return (T)Convert.ChangeType(value, typeof(T));
    }

    private static OracleDbType InferDbType(Type clrType)
    {
        Type underlying = Nullable.GetUnderlyingType(clrType) ?? clrType;
        if (underlying == typeof(int)) return OracleDbType.Int32;
        if (underlying == typeof(long)) return OracleDbType.Int64;
        if (underlying == typeof(short)) return OracleDbType.Int16;
        if (underlying == typeof(byte)) return OracleDbType.Byte;
        if (underlying == typeof(bool)) return OracleDbType.Boolean;
        if (underlying == typeof(string)) return OracleDbType.Varchar2;
        if (underlying == typeof(DateTime)) return OracleDbType.TimeStamp;
        if (underlying == typeof(decimal)) return OracleDbType.Decimal;
        if (underlying == typeof(double)) return OracleDbType.Double;
        if (underlying == typeof(float)) return OracleDbType.Single;
        if (underlying == typeof(Guid)) return OracleDbType.Raw;
        if (underlying == typeof(byte[])) return OracleDbType.Blob;
        return OracleDbType.Varchar2;
    }
}

/// <summary>
/// Opaque Oracle parameter configuration state.
/// </summary>
public struct OracleParameterState
{
    /// <summary>
    /// The configured Oracle database type, if any.
    /// </summary>
    public OracleDbType? DbType { get; set; }
}
