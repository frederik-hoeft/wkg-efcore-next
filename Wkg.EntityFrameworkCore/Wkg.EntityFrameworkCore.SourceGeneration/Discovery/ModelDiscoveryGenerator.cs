using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using Wkg.EntityFrameworkCore.SourceGeneration.Discovery.Contracts;
using Wkg.EntityFrameworkCore.SourceGeneration.Discovery.Emitters;
using Wkg.EntityFrameworkCore.SourceGeneration.Helpers;

namespace Wkg.EntityFrameworkCore.SourceGeneration.Discovery;

[Generator(LanguageNames.CSharp)]
public sealed class ModelDiscoveryGenerator : IIncrementalGenerator
{
    private static string ModelLoaderAttributeMetadataName => field ??= typeof(ModelLoaderAttribute).FullName
        ?? throw new InvalidOperationException($"{nameof(ModelLoaderAttribute)} must have a full name.");

    private static string ModelDiscoveryFilterAttributeMetadataName => field ??= typeof(ModelDiscoveryFilterAttribute<>).FullName
        ?? throw new InvalidOperationException($"{nameof(ModelDiscoveryFilterAttribute<>)} must have a full name.");

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(EmitCanonicalSources);

        IncrementalValuesProvider<ModelDiscoveryGeneratorModel> models = context.SyntaxProvider.ForAttributeWithMetadataName(
            ModelLoaderAttributeMetadataName,
            predicate: static (syntaxNode, _) => syntaxNode is ClassDeclarationSyntax,
            transform: static (attributeContext, _) => CreateModel(attributeContext));

        context.RegisterSourceOutput(
            models.Collect().Combine(context.CompilationProvider),
            static (sourceContext, input) =>
            {
                (ImmutableArray<ModelDiscoveryGeneratorModel> discoveredModels, Compilation compilation) = input;
                if (discoveredModels.IsEmpty)
                {
                    return;
                }

                ModelDiscoveryContractResolution contractResolution = ModelDiscoveryContractBindings.Resolve(compilation);
                foreach (Diagnostic diagnostic in contractResolution.Diagnostics)
                {
                    sourceContext.ReportDiagnostic(diagnostic);
                }

                if (contractResolution.Bindings is not { } contracts)
                {
                    return;
                }

                foreach (ModelDiscoveryGeneratorModel model in discoveredModels)
                {
                    ModelDiscoveryEmitter.EmitSource(sourceContext, model, compilation, contracts);
                }
            });
    }

    private static ModelDiscoveryGeneratorModel CreateModel(GeneratorAttributeSyntaxContext context)
    {
        INamedTypeSymbol targetClass = (INamedTypeSymbol)context.TargetSymbol;
        AttributeData loaderAttribute = context.Attributes.Single();
        ImmutableArray<AttributeData> filters =
        [
            .. targetClass.GetAttributes().Where(static attribute =>
                attribute.AttributeClass is { OriginalDefinition: { } originalDefinition }
                && originalDefinition.GetFullMetadataName() == ModelDiscoveryFilterAttributeMetadataName)
        ];

        return new ModelDiscoveryGeneratorModel(
            targetClass.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted)),
            targetClass,
            ModelDiscoveryOptions.FromAttributeData(loaderAttribute),
            filters);
    }

    private static void EmitCanonicalSources(IncrementalGeneratorPostInitializationContext context)
    {
        context.AddCanonicalSource<ModelLoaderAttribute>("ModelLoaderAttribute.g.cs");
        context.AddCanonicalSource<AssemblyDiscoveryFailureBehavior>("AssemblyDiscoveryFailureBehavior.g.cs");
        context.AddCanonicalSource(typeof(ModelDiscoveryFilterAttribute<>), "ModelDiscoveryFilterAttribute.g.cs");
        context.AddCanonicalSource<ModelDiscoveryContract>("ModelDiscoveryContract.g.cs");
    }
}

internal sealed record ModelDiscoveryGeneratorModel(
    string Namespace,
    INamedTypeSymbol Class,
    ModelDiscoveryOptions Options,
    ImmutableArray<AttributeData> Filters);

internal sealed record ModelConnection(INamedTypeSymbol Connector, ITypeSymbol Left, ITypeSymbol Right);

internal sealed record ModelDataSeed(INamedTypeSymbol Seeder, ITypeSymbol Model);
