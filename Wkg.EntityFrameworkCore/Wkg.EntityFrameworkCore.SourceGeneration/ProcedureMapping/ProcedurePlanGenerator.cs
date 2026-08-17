using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using Wkg.EntityFrameworkCore.ProcedureMapping.Generation;
using Wkg.EntityFrameworkCore.SourceGeneration.Discovery;
using Wkg.EntityFrameworkCore.SourceGeneration.Helpers;
using Wkg.EntityFrameworkCore.SourceGeneration.ProcedureMapping.Analysis;
using Wkg.EntityFrameworkCore.SourceGeneration.ProcedureMapping.Contracts;
using Wkg.EntityFrameworkCore.SourceGeneration.ProcedureMapping.Emission;
using Wkg.EntityFrameworkCore.SourceGeneration.ProcedureMapping.Grammar;

namespace Wkg.EntityFrameworkCore.SourceGeneration.ProcedureMapping;

[Generator(LanguageNames.CSharp)]
public sealed class ProcedurePlanGenerator : IIncrementalGenerator
{
    private static string ModelLoaderAttributeMetadataName => field ??= typeof(ModelLoaderAttribute).FullName
        ?? throw new InvalidOperationException($"{nameof(ModelLoaderAttribute)} must have a full name.");

    private static string ModelDiscoveryFilterAttributeMetadataName => field ??= typeof(ModelDiscoveryFilterAttribute<>).FullName
        ?? throw new InvalidOperationException($"{nameof(ModelDiscoveryFilterAttribute<>)} must have a full name.");

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static postInit =>
            postInit.AddCanonicalSource<ProcedureGenerationContract>("ProcedureGenerationContract.g.cs"));

        IncrementalValuesProvider<ConfigureCandidate> configureMethods = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: static (node, _) => IsConfigureMethod(node),
            transform: static (syntaxContext, cancellationToken) => CreateCandidate(syntaxContext, cancellationToken))
            .Where(static candidate => candidate is not null)!;

        IncrementalValuesProvider<ModelDiscoveryGeneratorModel> loaders = context.SyntaxProvider.ForAttributeWithMetadataName(
            ModelLoaderAttributeMetadataName,
            predicate: static (syntaxNode, _) => syntaxNode is ClassDeclarationSyntax,
            transform: static (attributeContext, _) => CreateLoaderModel(attributeContext));

        context.RegisterSourceOutput(
            configureMethods.Collect().Combine(loaders.Collect()).Combine(context.CompilationProvider),
            static (sourceContext, input) =>
            {
                ((ImmutableArray<ConfigureCandidate> candidates, ImmutableArray<ModelDiscoveryGeneratorModel> loaderModels), Compilation compilation) = input;
                ProcedureGenerationContractResolution contractResolution = ProcedureGenerationContractBindings.Resolve(compilation);
                foreach (Diagnostic diagnostic in contractResolution.Diagnostics)
                {
                    sourceContext.ReportDiagnostic(diagnostic);
                }

                if (contractResolution.Bindings is not { } contracts)
                {
                    return;
                }

                ProviderGrammarDiscovery grammarDiscovery = ProviderGrammarExplorer.Discover(compilation);
                foreach (Diagnostic diagnostic in grammarDiscovery.Diagnostics)
                {
                    sourceContext.ReportDiagnostic(diagnostic);
                }

                ImmutableArray<EmittedProcedurePlan>.Builder emitted = ImmutableArray.CreateBuilder<EmittedProcedurePlan>();
                foreach (ConfigureCandidate candidate in candidates)
                {
                    SemanticModel semanticModel = compilation.GetSemanticModel(candidate.Syntax.SyntaxTree);
                    if (semanticModel.GetDeclaredSymbol(candidate.Syntax, sourceContext.CancellationToken) is not IMethodSymbol method)
                    {
                        continue;
                    }

                    ConfigureSyntaxAnalyzer analyzer = new(semanticModel, grammarDiscovery.Grammar, contracts);
                    ProcedureMapping.Model.ProcedurePlanModel model = analyzer.Analyze(method, candidate.Syntax, sourceContext.CancellationToken);
                    EmittedProcedurePlan? plan = ProcedurePlanEmitter.Emit(sourceContext, model, contracts);
                    if (plan is not null)
                    {
                        emitted.Add(plan);
                    }
                }

                ImmutableArray<EmittedProcedurePlan> plans = emitted.ToImmutable();
                ProcedureLoaderEmitter.EmitModuleInitializer(sourceContext, plans, contracts);
                foreach (ModelDiscoveryGeneratorModel loader in loaderModels)
                {
                    ProcedureLoaderEmitter.Emit(sourceContext, loader, compilation, contracts, plans);
                }
            });
    }

    private static bool IsConfigureMethod(SyntaxNode node) =>
        node is MethodDeclarationSyntax { Identifier.ValueText: "Configure", ParameterList.Parameters.Count: 1 } method
        && method.Modifiers.Any(SyntaxKind.StaticKeyword);

    private static ConfigureCandidate? CreateCandidate(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        MethodDeclarationSyntax syntax = (MethodDeclarationSyntax)context.Node;
        if (context.SemanticModel.GetDeclaredSymbol(syntax, cancellationToken) is not IMethodSymbol method)
        {
            return null;
        }

        if (method.IsAbstract || method.ContainingType.TypeKind is TypeKind.Interface)
        {
            return null;
        }

        if (method.Parameters is not [{ Type: INamedTypeSymbol builderType }])
        {
            return null;
        }

        if (!IsProcedureBuilder(builderType))
        {
            return null;
        }

        return new ConfigureCandidate(syntax);
    }

    private static bool IsProcedureBuilder(INamedTypeSymbol builderType)
    {
        INamedTypeSymbol? current = builderType;
        while (current is not null)
        {
            if (current.GetAttributes().Any(static attribute =>
                attribute.AttributeClass?.OriginalDefinition.GetFullMetadataName() == typeof(ProcedureGrammarScopeAttribute).FullName)
                || current.Name.Contains("ProcedureBuilder", StringComparison.Ordinal))
            {
                return true;
            }

            current = current.BaseType;
        }

        return builderType.AllInterfaces.Any(static iface => iface.Name is "IProcedureBuilder");
    }

    private static ModelDiscoveryGeneratorModel CreateLoaderModel(GeneratorAttributeSyntaxContext context)
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
}

internal sealed record ConfigureCandidate(MethodDeclarationSyntax Syntax);
