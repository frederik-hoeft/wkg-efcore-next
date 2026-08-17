namespace Wkg.EntityFrameworkCore.ProcedureMapping.Generation;

/// <summary>
/// Links a fluent builder method to a compiled provider (or Core) intrinsic invoked from generated execution plans.
/// </summary>
/// <param name="intrinsicsType">The type that owns the intrinsic.</param>
/// <param name="memberName">The name of the static intrinsic method.</param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class TerminalIntrinsicAttribute(Type intrinsicsType, string memberName) : Attribute
{
    /// <summary>
    /// The type that owns the intrinsic.
    /// </summary>
    public Type IntrinsicsType { get; } = intrinsicsType;

    /// <summary>
    /// The name of the static intrinsic method.
    /// </summary>
    public string MemberName { get; } = memberName;
}
