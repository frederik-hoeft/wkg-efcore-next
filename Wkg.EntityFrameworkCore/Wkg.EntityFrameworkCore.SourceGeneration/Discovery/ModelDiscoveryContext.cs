using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using Wkg.EntityFrameworkCore.SourceGeneration.Discovery.Contracts;

namespace Wkg.EntityFrameworkCore.SourceGeneration.Discovery;

/// <summary>
/// Represents metadata and state for model discovery during a source generation run.
/// </summary>
internal sealed class ModelDiscoveryContext(
    ModelDiscoveryOptions options,
    ImmutableArray<AttributeData> filterAttributeData,
    ModelDiscoveryContractBindings contracts)
{
    private readonly Dictionary<string, int>? _assemblyTypeCounts = options.TargetAssemblies is { Length: > 0 } targetAssemblies
        ? targetAssemblies.ToDictionary(static name => name, static _ => 0)
        : null;
    private int _loadedTypeCount;

    public ModelDiscoveryOptions Options => options;

    public ImmutableArray<AttributeData> FilterAttributeData => filterAttributeData;

    public IEnumerable<ITypeSymbol> GetFilterAttributeTypes(ISymbol source, SourceProductionContext context)
    {
        foreach (AttributeData filterAttribute in filterAttributeData)
        {
            if (filterAttribute.AttributeClass is not { TypeArguments: [ITypeSymbol filterTypeSymbol] })
            {
                continue;
            }

            bool isValid = false;
            for (INamedTypeSymbol? baseType = filterTypeSymbol.BaseType; baseType is not null; baseType = baseType.BaseType)
            {
                if (SymbolEqualityComparer.Default.Equals(baseType, contracts.DatabaseEngineModelAttribute))
                {
                    isValid = true;
                    break;
                }
            }

            if (!isValid)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        id: "WKGLIBEFC003",
                        title: "Invalid model discovery filter attribute",
                        messageFormat: "The type argument '{0}' of the model discovery filter attribute must derive from DatabaseEngineModelAttribute.",
                        category: "Usage",
                        defaultSeverity: DiagnosticSeverity.Error,
                        isEnabledByDefault: true),
                    source.Locations.FirstOrDefault(),
                    filterTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
                continue;
            }

            yield return filterTypeSymbol;
        }
    }

    public void ReportDiscoveryResults(ISymbol source, SourceProductionContext context)
    {
        if (_loadedTypeCount is 0)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    id: "WKGLIBEFC004",
                    title: "No discoverable models found",
                    messageFormat: "No discoverable models implementing IDiscoverableModelConfiguration<T> were found in the specified assemblies.",
                    category: "Design",
                    defaultSeverity: options.GetDiagnosticSeverity(),
                    isEnabledByDefault: true),
                source.Locations.FirstOrDefault()));
        }

        if (_assemblyTypeCounts is null)
        {
            return;
        }

        foreach (KeyValuePair<string, int> assemblyTypeCount in _assemblyTypeCounts)
        {
            if (assemblyTypeCount.Value is not 0)
            {
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    id: "WKGLIBEFC005",
                    title: "No discoverable models found in assembly",
                    messageFormat: "Assembly '{0}' does not contain any discoverable models implementing IDiscoverableModelConfiguration<T>.",
                    category: "Design",
                    defaultSeverity: options.GetDiagnosticSeverity(),
                    isEnabledByDefault: true),
                source.Locations.FirstOrDefault(),
                assemblyTypeCount.Key));
        }
    }

    public DiagnosticSeverity GetDiagnosticSeverity() => options.GetDiagnosticSeverity();

    public void OnTypeDiscovered(INamedTypeSymbol type)
    {
        ++_loadedTypeCount;
        if (_assemblyTypeCounts is not null && _assemblyTypeCounts.ContainsKey(type.ContainingAssembly.Name))
        {
            ++_assemblyTypeCounts[type.ContainingAssembly.Name];
        }
    }
}
