namespace Wkg.EntityFrameworkCore.ProcedureMapping.Generation;

/// <summary>
/// Marks a fluent builder method as a Core-owned structural operation in the stored-procedure meta-grammar.
/// </summary>
/// <param name="role">The structural role implemented by the annotated method.</param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class StructuralOperationAttribute(StructuralRole role) : Attribute
{
    /// <summary>
    /// The structural role implemented by the annotated method.
    /// </summary>
    public StructuralRole Role { get; } = role;
}
