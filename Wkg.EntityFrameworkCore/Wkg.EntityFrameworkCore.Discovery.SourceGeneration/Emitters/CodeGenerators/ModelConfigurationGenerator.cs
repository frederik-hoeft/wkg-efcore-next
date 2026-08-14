using Microsoft.CodeAnalysis;
using System.Collections.Frozen;
using System.Text;
using Wkg.EntityFrameworkCore.Discovery.SourceGeneration.Discovery;

namespace Wkg.EntityFrameworkCore.Discovery.SourceGeneration.Emitters.CodeGenerators;

/// <summary>
/// Generates a model registration for a given model type.
/// </summary>
/// <param name="types">The frozen dictionary of type name mappings.</param>
internal sealed class ModelConfigurationGenerator(FrozenDictionary<string, string> types)
{
    private readonly FrozenDictionary<string, string> _types = types;

    public INamedConfigurationCode GenerateCode(INamedTypeSymbol modelSymbol)
    {
        IEnumerable<INamedTypeSymbol> baseModelSymbols = CompilationExplorer.GetBaseModelConfigurationSymbols(modelSymbol);
        return new ModelConfigurationCode(this, modelSymbol, baseModelSymbols);
    }

    private sealed class ModelConfigurationCode(ModelConfigurationGenerator generator, INamedTypeSymbol modelSymbol, IEnumerable<INamedTypeSymbol> baseModelSymbols) : NamedConfigurationCodeBase
    {
        public override INamedTypeSymbol Symbol => modelSymbol;

        public override void ResolveDependencies(FrozenDictionary<ITypeSymbol, INamedConfigurationCode> allConfigurations) { }

        public override IEnumerable<string> EmitSourceLines(ISymbol source, SourceProductionContext context)
        {
            string modelFullName = Symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            StringBuilder builder = string.IsNullOrEmpty(InstanceName)
                ? new()
                : new($"{generator._types["EntityTypeBuilder"]}<{modelFullName}> {InstanceName} = ");
            builder.Append($"{generator._types["EntityLoader"]}<{modelFullName}>.Configure(builder)");
            foreach (INamedTypeSymbol baseModelSymbol in baseModelSymbols)
            {
                builder.Append($".ConfigureBase<{modelFullName}, {baseModelSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>()");
            }
            builder.Append(".Register(context);");
            return [builder.ToString()];
        }
    }
}