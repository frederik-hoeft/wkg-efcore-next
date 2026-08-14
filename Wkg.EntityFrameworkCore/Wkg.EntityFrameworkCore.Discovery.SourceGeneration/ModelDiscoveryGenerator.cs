using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using Wkg.EntityFrameworkCore.Discovery.SourceGeneration;
using Wkg.EntityFrameworkCore.Discovery.SourceGeneration.Helpers;
using Wkg.EntityFrameworkCore.Discovery.SourceGeneration.Discovery;
using Wkg.EntityFrameworkCore.Discovery.SourceGeneration.Emitters;

namespace Wkg.EntityFrameworkCore.Discovery.SourceGeneration;

[Generator(LanguageNames.CSharp)]
public sealed class ModelDiscoveryGenerator : IIncrementalGenerator
{
    public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [
        new DiagnosticDescriptor(
            id: "WKGLIBEFC002",
            title: "Missing target assembly for model discovery",
            messageFormat: $"Target assembly '{{0}}' specified in the {nameof(ModelLoaderAttribute)} could not be found in the compilation.",
            category: "ModelDiscovery",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Ensure that the assembly name is spelled correctly and that the assembly is referenced by the project.")
    ];

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(EmitModelDiscoverySource);

        IncrementalValuesProvider<ModelDiscoveryGeneratorModel> pipeline = context.SyntaxProvider.ForAttributeWithMetadataName(
            typeof(ModelLoaderAttribute).FullName,
            predicate: static (syntaxNode, _) => syntaxNode is ClassDeclarationSyntax,
            transform: static (context, _) =>
            {
                ISymbol targetClass = context.TargetSymbol;
                AttributeData attributeData = targetClass.GetAttributes().Single(attr => attr.AttributeClass?.ToDisplayString() == typeof(ModelLoaderAttribute).FullName);
                ImmutableArray<AttributeData> filters = 
                [
                    .. targetClass.GetAttributes()
                    .Where(attr => attr.AttributeClass is { IsGenericType: true } attrClass 
                        && attrClass.ConstructUnboundGenericType().ToDisplayString() == $"{typeof(ModelDiscoveryFilterAttribute<>).Namespace}.{nameof(ModelDiscoveryFilterAttribute<>)}<>")
                ];
                ModelDiscoveryContext discoveryContext = new(ModelLoaderAttribute.FromAttributeData(attributeData), filters);
                CompilationExplorer explorer = new(context.SemanticModel.Compilation, discoveryContext);

                return new ModelDiscoveryGeneratorModel
                (
                    Namespace: targetClass.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted)),
                    Class: targetClass,
                    CompilationExplorer: explorer
                );
            }
        );
        context.RegisterSourceOutput(pipeline, ModelDiscoveryEmitter.EmitSource);
    }

    private static void EmitModelDiscoverySource(IncrementalGeneratorPostInitializationContext context)
    {
        context.AddEmbeddedSource<ModelLoaderAttribute>();
        context.AddEmbeddedSource<AssemblyDiscoveryFailureBehavior>();
        context.AddEmbeddedSource<ModelDiscoveryFilterAttribute<ModelLoaderAttribute>>(nameof(ModelDiscoveryFilterAttribute<>));
    }
}

internal sealed record ModelDiscoveryGeneratorModel(string Namespace, ISymbol Class, CompilationExplorer CompilationExplorer);

internal sealed record ModelConnection(INamedTypeSymbol Connector, ITypeSymbol Left, ITypeSymbol Right);

internal sealed record ModelDataSeed(INamedTypeSymbol Seeder, ITypeSymbol Model);