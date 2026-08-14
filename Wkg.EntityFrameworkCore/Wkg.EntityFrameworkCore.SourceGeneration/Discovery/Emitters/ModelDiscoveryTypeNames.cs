using Microsoft.CodeAnalysis;
using Wkg.EntityFrameworkCore.SourceGeneration.Discovery.Contracts;

namespace Wkg.EntityFrameworkCore.SourceGeneration.Discovery.Emitters;

/// <summary>
/// Strongly typed source-level names consumed by the model-discovery renderer.
/// </summary>
internal sealed record ModelDiscoveryTypeNames(
    string EntityTypeBuilder,
    string ModelBuilder,
    string ModelLoader,
    string EntityDiscoveryContext,
    string ModelConfiguration,
    string ModelConnection,
    string ModelDataSeed,
    string BaseModelConfiguration,
    string EntityDiscoveryHelpers,
    string EntityLoader,
    string EntityConnectionLoader,
    string EntityDataSeedLoader)
{
    private static readonly SymbolDisplayFormat s_contractTypeNameFormat =
        SymbolDisplayFormat.FullyQualifiedFormat.WithGenericsOptions(SymbolDisplayGenericsOptions.None);

    public static ModelDiscoveryTypeNames Create(ModelDiscoveryContractBindings contracts) => new(
        EntityTypeBuilder: "global::Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder",
        ModelBuilder: "global::Microsoft.EntityFrameworkCore.ModelBuilder",
        ModelLoader: GetName(contracts.ModelLoader),
        EntityDiscoveryContext: GetName(contracts.EntityDiscoveryContext),
        ModelConfiguration: GetName(contracts.ModelConfiguration),
        ModelConnection: GetName(contracts.ModelConnection),
        ModelDataSeed: GetName(contracts.ModelDataSeed),
        BaseModelConfiguration: GetName(contracts.BaseModelConfiguration),
        EntityDiscoveryHelpers: GetName(contracts.EntityDiscoveryHelpers),
        EntityLoader: "__EntityLoader",
        EntityConnectionLoader: "__EntityConnectionLoader",
        EntityDataSeedLoader: "__EntityDataSeedLoader");

    private static string GetName(INamedTypeSymbol symbol) =>
        symbol.ToDisplayString(s_contractTypeNameFormat);
}
