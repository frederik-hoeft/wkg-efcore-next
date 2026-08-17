using MySql.Data.MySqlClient;
using System.Data;
using System.Data.Common;

namespace Wkg.EntityFrameworkCore.MySql.ProcedureMapping.Generation;

/// <summary>
/// Compiled MySQL parameter helpers invoked by source-generated execution plans.
/// </summary>
public static class MySqlParameterIntrinsics
{
    /// <summary>
    /// Creates an empty MySQL parameter configuration state.
    /// </summary>
    public static MySqlParameterState Create() => new();

    /// <summary>
    /// Records the provider-specific database type.
    /// </summary>
    public static void HasDbType(ref MySqlParameterState state, MySqlDbType dbType) => state.DbType = dbType;

    /// <summary>
    /// Materializes a <see cref="MySqlParameter"/> from the accumulated configuration and runtime value.
    /// </summary>
    public static DbParameter Finalize(ref MySqlParameterState state, string name, ParameterDirection direction, int size, Type clrType, object? value)
    {
        MySqlDbType dbType = state.DbType ?? InferDbType(clrType);
        return new MySqlParameter(name, dbType)
        {
            Direction = direction,
            Size = size,
            Value = value ?? DBNull.Value
        };
    }

    /// <summary>
    /// Reads an output parameter value, including MySQL boolean coercions from <see cref="ulong"/>.
    /// </summary>
    public static T Store<T>(DbParameter parameter)
    {
        object? value = parameter.Value;
        if (value is null or DBNull)
        {
            return default!;
        }

        if (typeof(T) == typeof(bool) && value is ulong number)
        {
            return (T)(object)(number != 0UL);
        }

        if (value is T typed)
        {
            return typed;
        }

        return (T)Convert.ChangeType(value, typeof(T));
    }

    private static MySqlDbType InferDbType(Type clrType)
    {
        Type underlying = Nullable.GetUnderlyingType(clrType) ?? clrType;
        if (underlying == typeof(int)) return MySqlDbType.Int32;
        if (underlying == typeof(long)) return MySqlDbType.Int64;
        if (underlying == typeof(short)) return MySqlDbType.Int16;
        if (underlying == typeof(byte)) return MySqlDbType.UByte;
        if (underlying == typeof(bool)) return MySqlDbType.Byte;
        if (underlying == typeof(string)) return MySqlDbType.VarChar;
        if (underlying == typeof(DateTime)) return MySqlDbType.DateTime;
        if (underlying == typeof(decimal)) return MySqlDbType.Decimal;
        if (underlying == typeof(double)) return MySqlDbType.Double;
        if (underlying == typeof(float)) return MySqlDbType.Float;
        if (underlying == typeof(Guid)) return MySqlDbType.Guid;
        if (underlying == typeof(byte[])) return MySqlDbType.Blob;
        return MySqlDbType.VarChar;
    }
}

/// <summary>
/// Opaque MySQL parameter configuration state.
/// </summary>
public struct MySqlParameterState
{
    /// <summary>
    /// The configured MySQL database type, if any.
    /// </summary>
    public MySqlDbType? DbType { get; set; }
}
