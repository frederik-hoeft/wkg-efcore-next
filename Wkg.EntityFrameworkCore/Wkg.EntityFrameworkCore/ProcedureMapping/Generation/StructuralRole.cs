namespace Wkg.EntityFrameworkCore.ProcedureMapping.Generation;

/// <summary>
/// Core-owned structural roles in the stored-procedure configuration meta-grammar.
/// </summary>
public enum StructuralRole
{
    /// <summary>
    /// Sentinel value. Not a valid structural role.
    /// </summary>
    None = 0,

    /// <summary>
    /// Maps the command object to a stored procedure name.
    /// </summary>
    ToDatabaseProcedure = 1,

    /// <summary>
    /// Maps the command object to a database function name.
    /// </summary>
    ToDatabaseFunction = 2,

    /// <summary>
    /// Marks the mapped object as a database function.
    /// </summary>
    IsFunction = 3,

    /// <summary>
    /// Declares a procedure parameter bound to an I/O container member.
    /// </summary>
    Parameter = 4,

    /// <summary>
    /// Declares a result set of the specified CLR type.
    /// </summary>
    Returns = 5,

    /// <summary>
    /// Declares a scalar return-value parameter bound to an I/O container member.
    /// </summary>
    ReturnsScalar = 6,

    /// <summary>
    /// Declares a result column bound to a result-entity member.
    /// </summary>
    Column = 7,

    /// <summary>
    /// Configures the result as a multi-row collection.
    /// </summary>
    AsCollection = 8,

    /// <summary>
    /// Configures the result as a single row.
    /// </summary>
    AsSingle = 9,

    /// <summary>
    /// Sets the database name of a parameter or column.
    /// </summary>
    HasName = 10,

    /// <summary>
    /// Sets the ADO.NET parameter direction.
    /// </summary>
    HasDirection = 11,

    /// <summary>
    /// Sets the parameter size hint.
    /// </summary>
    HasSize = 12,

    /// <summary>
    /// Marks a result column as nullable.
    /// </summary>
    MayBeNull = 13,

    /// <summary>
    /// Applies a compile-time conversion from the reader value to the target member type.
    /// </summary>
    RequiresConversion = 14
}
