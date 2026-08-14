using Microsoft.CodeAnalysis;
using Wkg.EntityFrameworkCore.SourceGeneration.Helpers;

namespace Wkg.EntityFrameworkCore.SourceGeneration.Discovery.Emitters;

/// <summary>
/// Base class for named configuration code emitters.
/// </summary>
internal abstract class NamedConfigurationCodeBase : ConfigurationCodeBase, INamedConfigurationCode
{
    protected string? InstanceName { get; private set; }

    public abstract INamedTypeSymbol Symbol { get; }

    public virtual string MarkRequired() => InstanceName ??= SymbolNameGenerator.MakeUnique(SymbolNameGenerator.MakeCamelCase(Symbol.Name));
}
