using Microsoft.CodeAnalysis;

namespace Wkg.EntityFrameworkCore.SourceGeneration.Discovery.Emitters;

/// <summary>
/// The interface for named configuration code generators, i.e., those that are associated with an <see cref="INamedTypeSymbol"/>.
/// </summary>
internal interface INamedConfigurationCode : IConfigurationCode
{
    INamedTypeSymbol Symbol { get; }

    string MarkRequired();
}
