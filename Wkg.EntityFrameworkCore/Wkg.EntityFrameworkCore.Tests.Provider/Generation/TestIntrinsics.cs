using System.Data;
using System.Data.Common;

namespace Wkg.EntityFrameworkCore.Tests.Provider.Generation;

public static class TestProcedureIntrinsics
{
    public static TestProcedureState Create() => new();

    public static void HasSchema(ref TestProcedureState state, string schema) => state.Schema = schema;

    public static string BuildCommandText(ref TestProcedureState state, string procedureName, bool isFunction)
    {
        _ = isFunction;
        return string.IsNullOrEmpty(state.Schema) ? procedureName : $"{state.Schema}.{procedureName}";
    }
}

public struct TestProcedureState
{
    public string? Schema { get; set; }
}

public static class TestParameterIntrinsics
{
    public static TestParameterState Create() => new();

    public static void HasDbType(ref TestParameterState state, DbType dbType) => state.DbType = dbType;

    public static void Precision(ref TestParameterState state, int precision) => state.Precision = precision;

    public static void Scale(ref TestParameterState state, int scale) => state.Scale = scale;

    public static DbParameter Finalize(ref TestParameterState state, string name, ParameterDirection direction, int size, Type clrType, object? value)
    {
        _ = clrType;
        return new TestDbParameter
        {
            ParameterName = name,
            DbType = state.DbType ?? DbType.Object,
            Direction = direction,
            Size = size,
            Value = value ?? DBNull.Value,
            SourceColumn = state.Precision is int precision ? $"p={precision};s={state.Scale}" : string.Empty
        };
    }

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
}

public struct TestParameterState
{
    public DbType? DbType { get; set; }
    public int? Precision { get; set; }
    public int Scale { get; set; }
}

public static class TestColumnIntrinsics
{
    public static TestColumnState Create() => new();

    public static void GetAsInt32(ref TestColumnState state) => state.Kind = TestColumnReaderKind.Int32;

    public static void GetAsString(ref TestColumnState state) => state.Kind = TestColumnReaderKind.String;

    public static void GetAsBytes(ref TestColumnState state) => state.Kind = TestColumnReaderKind.Bytes;

    public static object? Read(ref TestColumnState state, DbDataReader reader, string columnName, bool isNullable)
    {
        int ordinal = reader.GetOrdinal(columnName);
        if (isNullable && reader.IsDBNull(ordinal))
        {
            return null;
        }

        return state.Kind switch
        {
            TestColumnReaderKind.Int32 => reader.GetInt32(ordinal),
            TestColumnReaderKind.String => reader.GetString(ordinal),
            TestColumnReaderKind.Bytes => (byte[])reader.GetValue(ordinal),
            _ => reader.GetValue(ordinal)
        };
    }
}

public struct TestColumnState
{
    public TestColumnReaderKind Kind { get; set; }
}

public enum TestColumnReaderKind
{
    Default = 0,
    Int32,
    String,
    Bytes
}
