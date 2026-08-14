using Microsoft.CodeAnalysis;
using System.Collections.Frozen;

namespace Wkg.EntityFrameworkCore.SourceGeneration.Discovery.Emitters.CodeGenerators;

/// <summary>
/// Generates model data seed configuration.
/// </summary>
internal sealed class ModelDataSeedConfigurationGenerator(ModelDiscoveryTypeNames types)
{
    public INamedConfigurationCode GenerateCode(ModelDataSeed dataSeed) => new ModelDataSeedConfigurationCode(types, dataSeed);

    private sealed class ModelDataSeedConfigurationCode(ModelDiscoveryTypeNames types, ModelDataSeed dataSeed) : NamedConfigurationCodeBase
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
            yield return $"{types.EntityDataSeedLoader}<{modelFullName}, {seederFullName}>.Configure({_modelBuilderInstance});";
        }
    }
}
