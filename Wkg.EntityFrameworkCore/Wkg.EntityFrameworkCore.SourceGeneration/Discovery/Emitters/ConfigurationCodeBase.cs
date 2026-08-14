using Microsoft.CodeAnalysis;
using System.Collections.Frozen;

namespace Wkg.EntityFrameworkCore.SourceGeneration.Discovery.Emitters;

/// <summary>
/// Base class for configuration code emitters.
/// </summary>
internal abstract class ConfigurationCodeBase : IConfigurationCode
{
    public abstract IEnumerable<string> EmitSourceLines(ISymbol source, SourceProductionContext context);

    public abstract void ResolveDependencies(FrozenDictionary<ITypeSymbol, INamedConfigurationCode> allConfigurations);
}
