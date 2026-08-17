namespace ContractDiscovery.Generator.Protocol;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
internal sealed class GeneratorContractRegistrationAttribute<TContract>(TContract contract) : Attribute where TContract : unmanaged, Enum
{
    public TContract Contract { get; } = contract;
}
