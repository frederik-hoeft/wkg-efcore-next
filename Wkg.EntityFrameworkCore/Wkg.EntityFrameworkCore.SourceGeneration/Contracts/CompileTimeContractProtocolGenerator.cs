using Microsoft.CodeAnalysis;
using Wkg.EntityFrameworkCore.SourceGeneration.Helpers;

namespace Wkg.EntityFrameworkCore.SourceGeneration.Contracts;

/// <summary>
/// Bootstraps the analyzer-wide compile-time contract registration protocol into consuming compilations.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class CompileTimeContractProtocolGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context) =>
        context.RegisterPostInitializationOutput(static postInit =>
            postInit.AddCanonicalSource(
                typeof(GeneratorContractRegistrationAttribute<>),
                "GeneratorContractRegistrationAttribute.g.cs"));
}
