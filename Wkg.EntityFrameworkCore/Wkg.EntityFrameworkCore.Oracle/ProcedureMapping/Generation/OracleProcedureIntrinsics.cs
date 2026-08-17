namespace Wkg.EntityFrameworkCore.Oracle.ProcedureMapping.Generation;

/// <summary>
/// Compiled Oracle procedure helpers invoked by source-generated execution plans.
/// </summary>
public static class OracleProcedureIntrinsics
{
    /// <summary>
    /// Creates an empty Oracle procedure configuration state.
    /// </summary>
    public static OracleProcedureState Create() => new();

    /// <summary>
    /// Records the Oracle package that contains the procedure.
    /// </summary>
    public static void InPackage(ref OracleProcedureState state, string packageName) => state.PackageName = packageName;

    /// <summary>
    /// Builds the ADO.NET command text, qualifying the procedure with its package when configured.
    /// </summary>
    public static string BuildCommandText(ref OracleProcedureState state, string procedureName, bool isFunction)
    {
        _ = isFunction;
        return string.IsNullOrEmpty(state.PackageName)
            ? procedureName
            : $"{state.PackageName}.{procedureName}";
    }
}

/// <summary>
/// Opaque Oracle procedure configuration state.
/// </summary>
public struct OracleProcedureState
{
    /// <summary>
    /// The configured package name, if any.
    /// </summary>
    public string? PackageName { get; set; }
}
