namespace Wkg.EntityFrameworkCore.ProcedureMapping.Generation;

/// <summary>
/// Marks a fluent method whose nested <c>Action&lt;TBuilder&gt;</c> argument is a composite configuration subtree.
/// </summary>
/// <remarks>
/// The generator flattens the nested lambda into terminal intrinsic calls against the current scope state.
/// The nested builder is a syntax-only receiver; it does not introduce a child state object.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class CompositeBuilderAttribute : Attribute;
