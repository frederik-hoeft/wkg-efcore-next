using Microsoft.CodeAnalysis;
using System.Collections.Frozen;

namespace Wkg.EntityFrameworkCore.Discovery.SourceGeneration.Emitters.CodeGenerators;

/// <summary>
/// Generates a model data seed registration for a given model data seed.
/// </summary>
/// <param name="types">The frozen dictionary of type name mappings.</param>
internal sealed class ModelDataSeedConfigurationGenerator(FrozenDictionary<string, string> types)
{
    private readonly FrozenDictionary<string, string> _types = types;

    public INamedConfigurationCode GenerateCode(ModelDataSeed dataSeed) => new DataSeedConfigurationCode(this, dataSeed);

    private sealed class DataSeedConfigurationCode(ModelDataSeedConfigurationGenerator generator, ModelDataSeed dataSeed) : NamedConfigurationCodeBase
    {
        private string? _modelBuilderInstance;

        public override INamedTypeSymbol Symbol => dataSeed.Seeder;

        public override void ResolveDependencies(FrozenDictionary<ITypeSymbol, INamedConfigurationCode> allConfigurations)
        {
            if (allConfigurations.TryGetValue(dataSeed.Model, out INamedConfigurationCode? model))
            {
                _modelBuilderInstance = model.MarkRequired();
            }
        }

        public override IEnumerable<string> EmitSourceLines(ISymbol source, SourceProductionContext context)
        {
            string seederFullName = Symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            string modelFullName = dataSeed.Model.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            _modelBuilderInstance ??= $"builder.Entity<{modelFullName}>()";
            yield return $"{generator._types["EntityDataSeedLoader"]}<{modelFullName}, {seederFullName}>.Configure({_modelBuilderInstance});";
        }
    }
}
