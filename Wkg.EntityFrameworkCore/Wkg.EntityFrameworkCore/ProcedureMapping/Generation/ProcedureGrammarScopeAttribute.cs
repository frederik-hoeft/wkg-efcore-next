namespace Wkg.EntityFrameworkCore.ProcedureMapping.Generation;

/// <summary>
/// Declares that the annotated builder type is a provider-grammar scope with the specified intrinsic factory.
/// </summary>
/// <param name="scope">The meta-grammar scope represented by the builder.</param>
/// <param name="intrinsicsType">The type that owns the initializer, terminals, and finalizer for this scope.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, AllowMultiple = false, Inherited = true)]
public sealed class ProcedureGrammarScopeAttribute(GrammarScopeKind scope, Type intrinsicsType) : Attribute
{
    /// <summary>
    /// The meta-grammar scope represented by the annotated builder.
    /// </summary>
    public GrammarScopeKind Scope { get; } = scope;

    /// <summary>
    /// The type that owns initializer, terminal, and finalizer methods for this scope.
    /// </summary>
    public Type IntrinsicsType { get; } = intrinsicsType;

    /// <summary>
    /// The name of the static initializer method on <see cref="IntrinsicsType"/>. Optional for scopes that do not thread provider state.
    /// </summary>
    public string? Initializer { get; set; }

    /// <summary>
    /// The name of the static finalizer method on <see cref="IntrinsicsType"/>. Required when the scope produces a runtime value.
    /// </summary>
    public string? Finalizer { get; set; }
}
