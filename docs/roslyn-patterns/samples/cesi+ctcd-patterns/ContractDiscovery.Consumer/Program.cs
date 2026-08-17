using ContractDiscovery.Generated;
using ContractDiscovery.Provider;

DemoService service = ResolvedContracts.CreateService();
Console.WriteLine(service.GetMessage());
