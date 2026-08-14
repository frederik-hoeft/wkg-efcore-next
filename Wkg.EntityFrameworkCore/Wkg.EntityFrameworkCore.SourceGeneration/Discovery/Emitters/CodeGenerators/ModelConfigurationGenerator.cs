using Microsoft.CodeAnalysis;
using System.Collections.Frozen;
using System.Text;

namespace Wkg.EntityFrameworkCore.SourceGeneration.Discovery.Emitters.CodeGenerators;

/// <summary>
/// Generates a model registration for a given model type.
/// </summary>
internal sealed class ModelConfigurationGenerator(ModelDiscoveryTypeNames types, CompilationExplorer explorer)
{
    public INamedConfigurationCode GenerateCode(INamedTypeSymbol modelSymbol) =>
        new ModelConfigurationCode(types, modelSymbol, explorer.GetBaseModelConfigurationSymbols(modelSymbol));

    private sealed class ModelConfigurationCode(
        ModelDiscoveryTypeNames types,
        INamedTypeSymbol modelSymbol,
        IEnumerable<INamedTypeSymbol> baseModelSymbols) : NamedConfigurationCodeBase
    {
        public override INamedTypeSymbol Symbol => modelSymbol;

        public override void ResolveDependencies(FrozenDictionary<ITypeSymbol, INamedConfigurationCode> allConfigurations) { }

        public override IEnumerable<string> EmitSourceLines(ISymbol source, SourceProductionContext context)
        {
            string modelFullName = Symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            StringBuilder builder = string.IsNullOrEmpty(InstanceName)
                ? new()
                : new($"{types.EntityTypeBuilder}<{modelFullName}> {InstanceName} = ");
            builder.Append($"{types.EntityLoader}<{modelFullName}>.Configure(builder)");
            foreach (INamedTypeSymbol baseModelSymbol in baseModelSymbols)
            {
                builder.Append($".ConfigureBase<{modelFullName}, {baseModelSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>()");
            }
            builder.Append(".Register(context);");
            return [builder.ToString()];
        }
    }
}
