namespace Wkg.EntityFrameworkCore.SourceGeneration.Contracts;

/// <summary>
/// Registers the annotated type as the implementation of a compile-time source-generation contract.
/// </summary>
/// <typeparam name="TContract">The enum vocabulary defining the contract family.</typeparam>
/// <param name="contract">The semantic contract implemented by the annotated type.</param>
[global::System.AttributeUsage(global::System.AttributeTargets.Class | global::System.AttributeTargets.Interface, AllowMultiple = true, Inherited = false)]
internal sealed class GeneratorContractRegistrationAttribute<TContract>(TContract contract) : global::System.Attribute where TContract : unmanaged, global::System.Enum
{
    public TContract Contract { get; } = contract;
}
