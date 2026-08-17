using ContractDiscovery.Generator.Protocol;

namespace ContractDiscovery.Provider;

[GeneratorContractRegistration<DemoContract>(DemoContract.Service)]
public sealed class DemoService
{
    public string GetMessage() => "Hello from the discovered provider contract.";
}
