using Microsoft.CodeAnalysis;
using System.Collections.Frozen;

namespace Wkg.EntityFrameworkCore.Discovery.SourceGeneration.Emitters;

/// <summary>
/// The base interface for all configuration code generators.
/// </summary>
internal interface IConfigurationCode
{
    IEnumerable<string> EmitSourceLines(ISymbol source, SourceProductionContext context);

    void ResolveDependencies(FrozenDictionary<ITypeSymbol, INamedConfigurationCode> allConfigurations);
}
