using Microsoft.CodeAnalysis;
using Wkg.EntityFrameworkCore.Discovery.SourceGeneration.Helpers;

namespace Wkg.EntityFrameworkCore.Discovery.SourceGeneration.Discovery;

/// <summary>
/// Explores a Roslyn Compilation to discover model configurations, connections, and data seeds based on marker interfaces.
/// </summary>
/// <param name="compilation">The compilation to explore.</param>
/// <param name="discoveryContext">The context describing discovery settings.</param>
internal sealed class CompilationExplorer(Compilation compilation, ModelDiscoveryContext discoveryContext)
{
    // scan for types implementing these marker interfaces
    private const string MODEL_MARKER_INTERFACE_FULL_NAME = "global::Wkg.EntityFrameworkCore.Configuration.IDiscoverableModelConfiguration<>";
    private const string MODEL_CONNECTION_MARKER_INTERFACE_FULL_NAME = "global::Wkg.EntityFrameworkCore.Configuration.IDiscoverableModelConnection<,,>";
    private const string BASE_MODEL_CONFIGURATION_INTERFACE_FULL_NAME = "global::Wkg.EntityFrameworkCore.Configuration.IDiscoverableBaseModelConfiguration<>";
    private const string DATA_SEED_MARKER_INTERFACE_FULL_NAME = "global::Wkg.EntityFrameworkCore.Configuration.IDiscoverableModelDataSeed<>";

    public ModelDiscoveryContext DiscoveryContext => discoveryContext;

    /// <summary>
    /// Gets candidate types from the compilation based on the discovery context settings.
    /// </summary>
    /// <param name="source">The source symbol for reporting diagnostics.</param>
    /// <param name="context">The source production context.</param>
    /// <returns>A collection of candidate named type symbols that qualify for further inspection.</returns>
    private IEnumerable<INamedTypeSymbol> GetCandidateTypes(ISymbol source, SourceProductionContext context)
    {
        IEnumerable<INamedTypeSymbol> allTypes;
        if (discoveryContext.Attribute.TargetAssemblies is { Length: > 0 } targetAssemblies)
        {
            // build a map of all assemblies in the compilation by name, names may not be unique
            Dictionary<string, List<IAssemblySymbol>> assemblies = compilation.References
                .Select(compilation.GetAssemblyOrModuleSymbol)
                .Union([source.ContainingAssembly], SymbolEqualityComparer.Default)
                .Distinct(SymbolEqualityComparer.Default)
                .OfType<IAssemblySymbol>()
                .GroupBy(asm => asm.Name)
                .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);
            foreach (string assemblyName in targetAssemblies)
            {
                // ensure the specified target assemblies are even present (detect typos and silent failures)
                if (!assemblies.ContainsKey(assemblyName))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            id: "WKGLIBEFC002",
                            title: "Missing target assembly for model discovery",
                            messageFormat: $"Target assembly '{{0}}' specified in the {nameof(ModelLoaderAttribute)} could not be found in the compilation.",
                            category: "ModelDiscovery",
                            discoveryContext.GetDiagnosticSeverity(),
                            isEnabledByDefault: true,
                            description: "Ensure that the assembly name is spelled correctly and that the assembly is referenced by the project."),
                        source.Locations.FirstOrDefault(),
                        assemblyName));
                }
            }
            // gather all types from the specified target assemblies
            allTypes = targetAssemblies.SelectMany(target =>
            {
                if (assemblies.TryGetValue(target, out List<IAssemblySymbol>? matches))
                {
                    return matches;
                }
                return [];
            }).SelectMany(asm => asm.GlobalNamespace.GetAllTypes());
        }
        else
        {
            // no target assemblies specified, scan all types in the current assembly only
            allTypes = compilation.Assembly.GlobalNamespace.GetAllTypes();
        }
        if (discoveryContext.FilterAttributeData.Length > 0)
        {
            // filter types based on the specified filter attributes, for example when operating on multiple DbContexts and database engines
            // this allows us to only consider types relevant to the current context
            HashSet<ITypeSymbol> filterAttributeTypes = new(discoveryContext.GetFilterAttributeTypes(source, context), SymbolEqualityComparer.Default);
            allTypes = allTypes.Where(type => type.GetAttributes().Any(attr => attr.AttributeClass is not null && filterAttributeTypes.Contains(attr.AttributeClass)));
        }
        return allTypes;
    }

    /// <summary>
    /// Discovers model configurations implementing the IDiscoverableModelConfiguration<T> marker interface from the candidate types.
    /// </summary>
    /// <param name="source">The source symbol for reporting diagnostics.</param>
    /// <param name="context">The source production context.</param>
    /// <returns>A collection of named type symbols representing discovered model configurations.</returns>
    public IEnumerable<INamedTypeSymbol> DiscoverModels(ISymbol source, SourceProductionContext context)
    {
        IEnumerable<INamedTypeSymbol> candidateTypes = GetCandidateTypes(source, context);
        List<INamedTypeSymbol> models = [];
        foreach (INamedTypeSymbol type in candidateTypes)
        {
            if (!type.IsReferenceType)
            {
                continue;
            }
            INamedTypeSymbol? modelInterface = type.Interfaces.FirstOrDefault(iface =>
                iface.IsGenericType
                && iface.ConstructUnboundGenericType().ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == MODEL_MARKER_INTERFACE_FULL_NAME
                && iface.TypeArguments is [ITypeSymbol self]
                && SymbolEqualityComparer.Default.Equals(self, type));
            if (modelInterface is null)
            {
                continue;
            }
            models.Add(type);
            discoveryContext.OnTypeDiscovered(type);
        }
        return models;
    }

    /// <summary>
    /// Discovers model connections implementing the IDiscoverableModelConnection<TSelf, TLeft, TRight> marker interface from the candidate types (n:m connections).
    /// </summary>
    /// <param name="source">The source symbol for reporting diagnostics.</param>
    /// <param name="context">The source production context.</param>
    /// <returns>A collection of ModelConnection records representing discovered model connections.</returns>
    public IEnumerable<ModelConnection> DiscoverModelConnections(ISymbol source, SourceProductionContext context)
    {
        IEnumerable<INamedTypeSymbol> candidateTypes = GetCandidateTypes(source, context);
        List<ModelConnection> connections = [];
        foreach (INamedTypeSymbol type in candidateTypes)
        {
            if (type.IsReferenceType && type.Interfaces.FirstOrDefault(i => i.IsGenericType
                && i.ConstructUnboundGenericType().ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == MODEL_CONNECTION_MARKER_INTERFACE_FULL_NAME) is
                {
                    TypeArguments: [ITypeSymbol self, ITypeSymbol left, ITypeSymbol right]
                } && SymbolEqualityComparer.Default.Equals(self, type))
            {
                connections.Add(new ModelConnection
                (
                    Connector: type,
                    Left: left,
                    Right: right
                ));
                discoveryContext.OnTypeDiscovered(type);
            }
        }
        return connections;
    }

    /// <summary>
    /// Discovers model data seeds implementing the IDiscoverableModelDataSeed<TModel> marker interface from the candidate types.
    /// </summary>
    /// <param name="source">The source symbol for reporting diagnostics.</param>
    /// <param name="context">The source production context.</param>
    /// <returns>A collection of ModelDataSeed records representing discovered model data seeds.</returns>
    public IEnumerable<ModelDataSeed> DiscoverDataSeeds(ISymbol source, SourceProductionContext context)
    {
        IEnumerable<INamedTypeSymbol> candidateTypes = GetCandidateTypes(source, context);
        List<ModelDataSeed> dataSeeds = [];
        foreach (INamedTypeSymbol type in candidateTypes)
        {
            if (type.Interfaces.FirstOrDefault(i => i.IsGenericType
                && i.ConstructUnboundGenericType().ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == DATA_SEED_MARKER_INTERFACE_FULL_NAME) is
                {
                    TypeArguments: [ITypeSymbol model]
                })
            {
                dataSeeds.Add(new ModelDataSeed
                (
                    Seeder: type,
                    Model: model
                ));
                discoveryContext.OnTypeDiscovered(type);
            }
        }
        return dataSeeds;
    }

    /// <summary>
    /// Traverses the inheritance hierarchy of the provided <paramref name="modelSymbol"/> to find all base classes implementing the
    /// IDiscoverableBaseModelConfiguration<T> interface where T is the base class itself.
    /// </summary>
    /// <param name="modelSymbol">The model type symbol to inspect.</param>
    /// <returns>A collection of all discovered parent classes that implement the base model configuration interface.</returns>
    public static IEnumerable<INamedTypeSymbol> GetBaseModelConfigurationSymbols(INamedTypeSymbol modelSymbol)
    {
        List<INamedTypeSymbol> baseConfigurations = [];
        
        // Traverse up the inheritance hierarchy
        for (INamedTypeSymbol? type = modelSymbol.BaseType; type is { SpecialType: not SpecialType.System_Object }; type = type.BaseType)
        {
            // Check if this base type implements IDiscoverableBaseModelConfiguration<T> where T is the base type itself
            INamedTypeSymbol? baseConfigurationInterface = type.Interfaces.FirstOrDefault(i => 
                i.IsGenericType
                && i.ConstructUnboundGenericType().ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == BASE_MODEL_CONFIGURATION_INTERFACE_FULL_NAME
                && i.TypeArguments is [ITypeSymbol self]
                && SymbolEqualityComparer.Default.Equals(self, type));
            
            if (baseConfigurationInterface is not null)
            {
                baseConfigurations.Add(type);
            }
        }
        
        return baseConfigurations;
    }
}
