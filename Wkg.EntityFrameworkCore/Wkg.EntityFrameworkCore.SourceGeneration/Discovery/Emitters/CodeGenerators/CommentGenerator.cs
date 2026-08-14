using Microsoft.CodeAnalysis;
using System.Collections.Frozen;

namespace Wkg.EntityFrameworkCore.SourceGeneration.Discovery.Emitters.CodeGenerators;

/// <summary>
/// Generates a single-line comment.
/// </summary>
internal sealed class CommentGenerator
{
    public IConfigurationCode GenerateCode(string comment) => new CommentCode(comment);

    private sealed class CommentCode(string comment) : ConfigurationCodeBase
    {
        public override IEnumerable<string> EmitSourceLines(ISymbol source, SourceProductionContext context) => [$"// {comment}"];

        public override void ResolveDependencies(FrozenDictionary<ITypeSymbol, INamedConfigurationCode> allConfigurations) { }
    }
}
