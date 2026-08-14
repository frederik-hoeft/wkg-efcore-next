using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using Wkg.EntityFrameworkCore.SourceGeneration.Contracts;

namespace Wkg.EntityFrameworkCore.SourceGeneration.Discovery.Contracts;

/// <summary>
/// Typed, validated Roslyn symbol bindings for the runtime roles consumed by model discovery generation.
/// </summary>
internal sealed record ModelDiscoveryContractBindings(
    INamedTypeSymbol ModelLoader,
    INamedTypeSymbol EntityDiscoveryContext,
    INamedTypeSymbol ModelConfiguration,
    INamedTypeSymbol ModelConnection,
    INamedTypeSymbol ModelDataSeed,
    INamedTypeSymbol BaseModelConfiguration,
    INamedTypeSymbol DiscoverableModelConfiguration,
    INamedTypeSymbol DiscoverableModelConnection,
    INamedTypeSymbol DiscoverableBaseModelConfiguration,
    INamedTypeSymbol DiscoverableModelDataSeed,
    INamedTypeSymbol EntityDiscoveryHelpers,
    INamedTypeSymbol DatabaseEngineModelAttribute)
{
    public static ModelDiscoveryContractResolution Resolve(Compilation compilation)
    {
        CompileTimeContractResolution<ModelDiscoveryContract> raw = CompileTimeContractResolver.Resolve<ModelDiscoveryContract>(compilation);
        ImmutableArray<Diagnostic>.Builder diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

        foreach (MalformedContractRegistration malformed in raw.MalformedRegistrations)
        {
            diagnostics.Add(Diagnostic.Create(
                ModelDiscoveryContractDiagnostics.MalformedRegistration,
                malformed.Provider.Locations.FirstOrDefault(),
                malformed.Provider.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
        }

        foreach (DuplicateContractRegistration<ModelDiscoveryContract> duplicate in raw.Duplicates)
        {
            diagnostics.Add(Diagnostic.Create(
                ModelDiscoveryContractDiagnostics.DuplicateRegistration,
                duplicate.Providers[0].Locations.FirstOrDefault(),
                duplicate.Contract,
                string.Join(", ", duplicate.Providers.Select(static provider => provider.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))));
        }

        Dictionary<ModelDiscoveryContract, INamedTypeSymbol> contracts = raw.Contracts.ToDictionary(static pair => pair.Key, static pair => pair.Value);
        if (raw.RegistrationCount is 0)
        {
            diagnostics.Add(Diagnostic.Create(
                ModelDiscoveryContractDiagnostics.MissingRegistration,
                Location.None,
                "ModelDiscovery"));
        }
        else
        {
            foreach (ModelDiscoveryContract contract in Enum.GetValues(typeof(ModelDiscoveryContract)).Cast<ModelDiscoveryContract>())
            {
                if (!contracts.ContainsKey(contract) && !raw.Duplicates.Any(duplicate => EqualityComparer<ModelDiscoveryContract>.Default.Equals(duplicate.Contract, contract)))
                {
                    diagnostics.Add(Diagnostic.Create(
                        ModelDiscoveryContractDiagnostics.MissingRegistration,
                        Location.None,
                        contract));
                }
            }
        }

        ValidateShape(contracts, ModelDiscoveryContract.ModelLoader, TypeKind.Interface, 0, diagnostics);
        ValidateShape(contracts, ModelDiscoveryContract.EntityDiscoveryContext, TypeKind.Interface, 0, diagnostics);
        ValidateShape(contracts, ModelDiscoveryContract.ModelConfiguration, TypeKind.Interface, 1, diagnostics);
        ValidateShape(contracts, ModelDiscoveryContract.ModelConnection, TypeKind.Interface, 3, diagnostics);
        ValidateShape(contracts, ModelDiscoveryContract.ModelDataSeed, TypeKind.Interface, 1, diagnostics);
        ValidateShape(contracts, ModelDiscoveryContract.BaseModelConfiguration, TypeKind.Interface, 1, diagnostics);
        ValidateShape(contracts, ModelDiscoveryContract.DiscoverableModelConfiguration, TypeKind.Interface, 1, diagnostics);
        ValidateShape(contracts, ModelDiscoveryContract.DiscoverableModelConnection, TypeKind.Interface, 3, diagnostics);
        ValidateShape(contracts, ModelDiscoveryContract.DiscoverableBaseModelConfiguration, TypeKind.Interface, 1, diagnostics);
        ValidateShape(contracts, ModelDiscoveryContract.DiscoverableModelDataSeed, TypeKind.Interface, 1, diagnostics);
        ValidateShape(contracts, ModelDiscoveryContract.EntityDiscoveryHelpers, TypeKind.Class, 0, diagnostics);
        ValidateShape(contracts, ModelDiscoveryContract.DatabaseEngineModelAttribute, TypeKind.Class, 0, diagnostics);

        if (diagnostics.Count > 0)
        {
            return new ModelDiscoveryContractResolution(null, diagnostics.ToImmutable());
        }

        return new ModelDiscoveryContractResolution(
            new ModelDiscoveryContractBindings(
                contracts[ModelDiscoveryContract.ModelLoader],
                contracts[ModelDiscoveryContract.EntityDiscoveryContext],
                contracts[ModelDiscoveryContract.ModelConfiguration],
                contracts[ModelDiscoveryContract.ModelConnection],
                contracts[ModelDiscoveryContract.ModelDataSeed],
                contracts[ModelDiscoveryContract.BaseModelConfiguration],
                contracts[ModelDiscoveryContract.DiscoverableModelConfiguration],
                contracts[ModelDiscoveryContract.DiscoverableModelConnection],
                contracts[ModelDiscoveryContract.DiscoverableBaseModelConfiguration],
                contracts[ModelDiscoveryContract.DiscoverableModelDataSeed],
                contracts[ModelDiscoveryContract.EntityDiscoveryHelpers],
                contracts[ModelDiscoveryContract.DatabaseEngineModelAttribute]),
            diagnostics.ToImmutable());
    }

    private static void ValidateShape(
        IReadOnlyDictionary<ModelDiscoveryContract, INamedTypeSymbol> contracts,
        ModelDiscoveryContract contract,
        TypeKind expectedKind,
        int expectedArity,
        ImmutableArray<Diagnostic>.Builder diagnostics)
    {
        if (!contracts.TryGetValue(contract, out INamedTypeSymbol? symbol)
            || symbol.TypeKind == expectedKind && symbol.Arity == expectedArity)
        {
            return;
        }

        diagnostics.Add(Diagnostic.Create(
            ModelDiscoveryContractDiagnostics.InvalidShape,
            symbol.Locations.FirstOrDefault(),
            contract,
            symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            expectedKind,
            expectedArity));
    }
}

internal sealed record ModelDiscoveryContractResolution(
    ModelDiscoveryContractBindings? Bindings,
    ImmutableArray<Diagnostic> Diagnostics);

internal static class ModelDiscoveryContractDiagnostics
{
    public static DiagnosticDescriptor MalformedRegistration { get; } = new(
        id: "WKGLIBEFC006",
        title: "Malformed model discovery source-generation contract registration",
        messageFormat: "Type '{0}' contains a malformed model discovery source-generation contract registration.",
        category: "SourceGenerationContracts",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor DuplicateRegistration { get; } = new(
        id: "WKGLIBEFC007",
        title: "Model discovery source-generation contract is registered more than once",
        messageFormat: "Contract '{0}' is registered by multiple types: {1}.",
        category: "SourceGenerationContracts",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor MissingRegistration { get; } = new(
        id: "WKGLIBEFC008",
        title: "Required model discovery source-generation contract is missing",
        messageFormat: "Required model discovery source-generation contract '{0}' is not registered by the referenced runtime assemblies.",
        category: "SourceGenerationContracts",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor InvalidShape { get; } = new(
        id: "WKGLIBEFC009",
        title: "Model discovery source-generation contract has an invalid type shape",
        messageFormat: "Contract '{0}' resolved to '{1}', but expected a {2} with generic arity {3}.",
        category: "SourceGenerationContracts",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
