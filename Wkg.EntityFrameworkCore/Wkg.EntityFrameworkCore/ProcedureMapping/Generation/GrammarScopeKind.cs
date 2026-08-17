namespace Wkg.EntityFrameworkCore.ProcedureMapping.Generation;

/// <summary>
/// Identifies a provider-grammar scope in Core's stored-procedure meta-grammar.
/// </summary>
public enum GrammarScopeKind
{
    /// <summary>
    /// Sentinel value. Not a valid grammar scope.
    /// </summary>
    None = 0,

    /// <summary>
    /// Procedure-level configuration (command text, packages, and other procedure-wide settings).
    /// </summary>
    Procedure = 1,

    /// <summary>
    /// Parameter-level configuration (provider DbType and other parameter settings).
    /// </summary>
    Parameter = 2,

    /// <summary>
    /// Result-set-level configuration.
    /// </summary>
    Result = 3,

    /// <summary>
    /// Result-column-level configuration (readers, provider DbType, and conversions).
    /// </summary>
    Column = 4
}
