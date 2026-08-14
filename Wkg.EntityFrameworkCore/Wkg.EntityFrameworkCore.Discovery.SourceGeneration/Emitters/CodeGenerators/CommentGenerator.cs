using Microsoft.CodeAnalysis;
using System.Collections.Frozen;

namespace Wkg.EntityFrameworkCore.Discovery.SourceGeneration.Emitters.CodeGenerators;

/// <summary>
/// Generates a single-line comment.
/// </summary>
/// <param name="types">The frozen dictionary of type name mappings.</param>
internal sealed class CommentGenerator(FrozenDictionary<string, string> types)
{
    private readonly FrozenDictionary<string, string> _types = types;

    public IConfigurationCode GenerateCode(string comment) => new CommentCode(() => comment);

    public IConfigurationCode GenerateCode(Func<FrozenDictionary<string, string>, string> commentFactory) => new CommentCode(() => commentFactory.Invoke(_types));

    private sealed class CommentCode(Func<string> commentProvider) : ConfigurationCodeBase
    {
        public override IEnumerable<string> EmitSourceLines(ISymbol source, SourceProductionContext context) => [$"// {commentProvider.Invoke()}"];

        public override void ResolveDependencies(FrozenDictionary<ITypeSymbol, INamedConfigurationCode> allConfigurations) { }
    }
}