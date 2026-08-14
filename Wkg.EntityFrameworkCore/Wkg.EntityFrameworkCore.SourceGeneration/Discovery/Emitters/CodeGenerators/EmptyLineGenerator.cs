using Microsoft.CodeAnalysis;
using System.Collections.Frozen;

namespace Wkg.EntityFrameworkCore.SourceGeneration.Discovery.Emitters.CodeGenerators;

/// <summary>
/// Generates an empty line.
/// </summary>
internal sealed class EmptyLineGenerator
{
    private readonly EmptyLineCode _emptyLineCode = new();

    public IConfigurationCode GenerateCode() => _emptyLineCode;

    private sealed class EmptyLineCode : ConfigurationCodeBase
    {
        private readonly string[] _emptyLine = [string.Empty];

        public override IEnumerable<string> EmitSourceLines(ISymbol source, SourceProductionContext context) => _emptyLine;

        public override void ResolveDependencies(FrozenDictionary<ITypeSymbol, INamedConfigurationCode> allConfigurations) { }
    }
}
