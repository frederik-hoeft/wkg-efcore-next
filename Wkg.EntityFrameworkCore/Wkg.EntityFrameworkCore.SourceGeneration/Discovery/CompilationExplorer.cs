using Microsoft.CodeAnalysis;
using Wkg.EntityFrameworkCore.SourceGeneration.Discovery.Contracts;
using Wkg.EntityFrameworkCore.SourceGeneration.Helpers;

namespace Wkg.EntityFrameworkCore.SourceGeneration.Discovery;

/// <summary>
/// Explores a Roslyn compilation to discover model configurations, connections, and data seeds based on registered marker-interface contracts.
/// </summary>
internal sealed class CompilationExplorer(Compilation compilation, ModelDiscoveryContext discoveryContext, ModelDiscoveryContractBindings contracts)
{
    public ModelDiscoveryContext DiscoveryContext => discoveryContext;

    private IEnumerable<INamedTypeSymbol> GetCandidateTypes(ISymbol source, SourceProductionContext context)
    {
        IEnumerable<INamedTypeSymbol> allTypes;
        if (discoveryContext.Options.TargetAssemblies is { Length: > 0 } targetAssemblies)
        {
            Dictionary<string, List<IAssemblySymbol>> assemblies = compilation.References
                .Select(compilation.GetAssemblyOrModuleSymbol)
                .Union([source.ContainingAssembly], SymbolEqualityComparer.Default)
                .Distinct(SymbolEqualityComparer.Default)
                .OfType<IAssemblySymbol>()
                .GroupBy(static assembly => assembly.Name)
                .ToDictionary(static group => group.Key, static group => group.ToList(), StringComparer.Ordinal);

            foreach (string assemblyName in targetAssemblies)
            {
                if (assemblies.ContainsKey(assemblyName))
                {
                    continue;
                }

                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        id: "WKGLIBEFC002",
                        title: "Missing target assembly for model discovery",
                        messageFormat: $"Target assembly '{{0}}' specified in the {nameof(ModelLoaderAttribute)} could not be found in the compilation.",
                        category: "ModelDiscovery",
                        defaultSeverity: discoveryContext.GetDiagnosticSeverity(),
                        isEnabledByDefault: true,
                        description: "Ensure that the assembly name is spelled correctly and that the assembly is referenced by the project."),
                    source.Locations.FirstOrDefault(),
                    assemblyName));
            }

            allTypes = targetAssemblies
                .SelectMany(target => assemblies.TryGetValue(target, out List<IAssemblySymbol>? matches) ? matches : [])
                .SelectMany(static assembly => assembly.GlobalNamespace.GetAllTypes());
        }
        else
        {
            allTypes = compilation.Assembly.GlobalNamespace.GetAllTypes();
        }

        if (discoveryContext.FilterAttributeData.Length > 0)
        {
            HashSet<ITypeSymbol> filterAttributeTypes = new(discoveryContext.GetFilterAttributeTypes(source, context), SymbolEqualityComparer.Default);
            allTypes = allTypes.Where(type => type.GetAttributes().Any(attribute =>
                attribute.AttributeClass is not null && filterAttributeTypes.Contains(attribute.AttributeClass)));
        }

        return allTypes;
    }

    public IEnumerable<INamedTypeSymbol> DiscoverModels(ISymbol source, SourceProductionContext context)
    {
        foreach (INamedTypeSymbol type in GetCandidateTypes(source, context))
        {
            if (!type.IsReferenceType)
            {
                continue;
            }

            INamedTypeSymbol? modelInterface = type.Interfaces.FirstOrDefault(iface =>
                iface.IsConstructedFrom(contracts.DiscoverableModelConfiguration)
                && iface.TypeArguments is [ITypeSymbol self]
                && SymbolEqualityComparer.Default.Equals(self, type));
            if (modelInterface is null)
            {
                continue;
            }

            discoveryContext.OnTypeDiscovered(type);
            yield return type;
        }
    }

    public IEnumerable<ModelConnection> DiscoverModelConnections(ISymbol source, SourceProductionContext context)
    {
        foreach (INamedTypeSymbol type in GetCandidateTypes(source, context))
        {
            if (!type.IsReferenceType)
            {
                continue;
            }

            INamedTypeSymbol? connectionInterface = type.Interfaces.FirstOrDefault(iface => iface.IsConstructedFrom(contracts.DiscoverableModelConnection));
            if (connectionInterface is not
                {
                    TypeArguments: [ITypeSymbol self, ITypeSymbol left, ITypeSymbol right]
                }
                || !SymbolEqualityComparer.Default.Equals(self, type))
            {
                continue;
            }

            discoveryContext.OnTypeDiscovered(type);
            yield return new ModelConnection(type, left, right);
        }
    }

    public IEnumerable<ModelDataSeed> DiscoverDataSeeds(ISymbol source, SourceProductionContext context)
    {
        foreach (INamedTypeSymbol type in GetCandidateTypes(source, context))
        {
            INamedTypeSymbol? dataSeedInterface = type.Interfaces.FirstOrDefault(iface => iface.IsConstructedFrom(contracts.DiscoverableModelDataSeed));
            if (dataSeedInterface is not { TypeArguments: [ITypeSymbol model] })
            {
                continue;
            }

            discoveryContext.OnTypeDiscovered(type);
            yield return new ModelDataSeed(type, model);
        }
    }

    public IEnumerable<INamedTypeSymbol> GetBaseModelConfigurationSymbols(INamedTypeSymbol modelSymbol)
    {
        for (INamedTypeSymbol? type = modelSymbol.BaseType; type is { SpecialType: not SpecialType.System_Object }; type = type.BaseType)
        {
            INamedTypeSymbol? baseConfigurationInterface = type.Interfaces.FirstOrDefault(iface =>
                iface.IsConstructedFrom(contracts.DiscoverableBaseModelConfiguration)
                && iface.TypeArguments is [ITypeSymbol self]
                && SymbolEqualityComparer.Default.Equals(self, type));
            if (baseConfigurationInterface is not null)
            {
                yield return type;
            }
        }
    }
}
