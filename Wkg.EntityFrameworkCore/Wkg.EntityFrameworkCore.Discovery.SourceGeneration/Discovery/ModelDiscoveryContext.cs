using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using Wkg.EntityFrameworkCore.Discovery.SourceGeneration;
using Wkg.EntityFrameworkCore.Discovery.SourceGeneration.Helpers;

namespace Wkg.EntityFrameworkCore.Discovery.SourceGeneration.Discovery;

/// <summary>
/// Represents metadata and state for model discovery during a source generation run.
/// </summary>
/// <param name="attribute">The <see cref="ModelLoaderAttribute"/> instance decorating the source model loader that initiated model discovery.</param>
/// <param name="filterAttributeData">The filter attributes applied to the source model loader.</param>
internal sealed class ModelDiscoveryContext(ModelLoaderAttribute attribute, ImmutableArray<AttributeData> filterAttributeData)
{
    private const string DATABASE_ENGINE_MODEL_ATTRIBUTE_FULL_NAME = "global::Wkg.EntityFrameworkCore.Configuration.Reflection.Attributes.DatabaseEngineModelAttribute";

    // tracking discovered types per assembly to validate that each target assembly contributed discoverable models, helps identify misconfigurations
    private readonly Dictionary<string, int>? _assemblyTypeCounts = attribute.TargetAssemblies is { Length: > 0 } targetAssemblies
        ? targetAssemblies.ToDictionary(name => name, _ => 0)
        : null;
    // simple counter to detect overall discovery failure (nothing found in any assembly)
    private int _loadedTypeCount = 0;

    public ModelLoaderAttribute Attribute => attribute;

    public ImmutableArray<AttributeData> FilterAttributeData => filterAttributeData;

    /// <summary>
    /// Retrieves the type symbols of the database engine model attributes specified in the filter attributes.
    /// </summary>
    /// <param name="source">The source symbol being analyzed.</param>
    /// <param name="context">The source production context.</param>
    /// <returns>An enumerable of <see cref="ITypeSymbol"/> representing the filter attribute types.</returns>
    public IEnumerable<ITypeSymbol> GetFilterAttributeTypes(ISymbol source, SourceProductionContext context)
    {
        // we basically need to extract the generic type argument from each attribute instance
        foreach (AttributeData filterAttributeData in filterAttributeData)
        {
            if (filterAttributeData.AttributeClass is { TypeArguments: [ITypeSymbol filterTypeSymbol] })
            {
                // ensure the attribute type derives from DatabaseEngineModelAttribute, since we can't enforce this through generic constraints (we don't hold a strong reference to that assembly)
                bool isValid = false;
                for (INamedTypeSymbol? baseType = filterTypeSymbol.BaseType; baseType is not null; baseType = baseType.BaseType)
                {
                    if (baseType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == DATABASE_ENGINE_MODEL_ATTRIBUTE_FULL_NAME)
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
                        source.Locations[0],
                        filterTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
                    continue;
                }
                yield return filterTypeSymbol;
            }
        }
    }

    /// <summary>
    /// Reports diagnostics based on the results of model discovery.
    /// </summary>
    /// <param name="source">The source symbol being analyzed.</param>
    /// <param name="context">The source production context.</param>
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
                    defaultSeverity: attribute.GetDiagnosticSeverity(),
                    isEnabledByDefault: true),
                source.Locations[0]));
        }
        if (_assemblyTypeCounts is not null)
        {
            foreach (KeyValuePair<string, int> kvp in _assemblyTypeCounts)
            {
                string assemblyName = kvp.Key;
                int typeCount = kvp.Value;
                if (typeCount is 0)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            id: "WKGLIBEFC005",
                            title: "No discoverable models found in assembly",
                            messageFormat: "Assembly '{0}' does not contain any discoverable models implementing IDiscoverableModelConfiguration<T>.",
                            category: "Design",
                            defaultSeverity: attribute.GetDiagnosticSeverity(),
                            isEnabledByDefault: true),
                        source.Locations[0],
                        assemblyName));
                }
            }
        }
    }

    /// <summary>
    /// Gets the diagnostic severity level specified in the model loader attribute.
    /// </summary>
    public DiagnosticSeverity GetDiagnosticSeverity() => attribute.GetDiagnosticSeverity();

    /// <summary>
    /// Records the discovery of a type.
    /// </summary>
    /// <param name="type">The discovered type symbol.</param>
    public void OnTypeDiscovered(INamedTypeSymbol type)
    {
        ++_loadedTypeCount;
        if (_assemblyTypeCounts is not null)
        {
            ++_assemblyTypeCounts[type.ContainingAssembly.Name];
        }
    }
}