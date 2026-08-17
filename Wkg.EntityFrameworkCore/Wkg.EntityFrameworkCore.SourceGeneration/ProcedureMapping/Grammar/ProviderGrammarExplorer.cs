using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using Wkg.EntityFrameworkCore.ProcedureMapping.Generation;
using Wkg.EntityFrameworkCore.SourceGeneration.Helpers;
using Wkg.EntityFrameworkCore.SourceGeneration.ProcedureMapping.Diagnostics;

namespace Wkg.EntityFrameworkCore.SourceGeneration.ProcedureMapping.Grammar;

internal static class ProviderGrammarExplorer
{
    private static string ScopeAttributeName => field ??= typeof(ProcedureGrammarScopeAttribute).FullName
        ?? throw new InvalidOperationException($"{nameof(ProcedureGrammarScopeAttribute)} must have a full name.");

    private static string StructuralAttributeName => field ??= typeof(StructuralOperationAttribute).FullName
        ?? throw new InvalidOperationException($"{nameof(StructuralOperationAttribute)} must have a full name.");

    private static string TerminalAttributeName => field ??= typeof(TerminalIntrinsicAttribute).FullName
        ?? throw new InvalidOperationException($"{nameof(TerminalIntrinsicAttribute)} must have a full name.");

    private static string CompositeAttributeName => field ??= typeof(CompositeBuilderAttribute).FullName
        ?? throw new InvalidOperationException($"{nameof(CompositeBuilderAttribute)} must have a full name.");

    public static ProviderGrammarDiscovery Discover(Compilation compilation)
    {
        ImmutableArray<GrammarScopeBinding>.Builder scopes = ImmutableArray.CreateBuilder<GrammarScopeBinding>();
        ImmutableDictionary<string, GrammarOperation>.Builder operations = ImmutableDictionary.CreateBuilder<string, GrammarOperation>(StringComparer.Ordinal);
        ImmutableArray<Diagnostic>.Builder diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

        foreach (IAssemblySymbol assembly in compilation.GetAllAssemblies())
        {
            foreach (INamedTypeSymbol type in assembly.GlobalNamespace.GetAllTypes())
            {
                TryAddScope(type, scopes, diagnostics);
                foreach (IMethodSymbol method in type.GetMembers().OfType<IMethodSymbol>())
                {
                    TryAddOperation(method, operations, diagnostics);
                }
            }
        }

        return new ProviderGrammarDiscovery(
            new ProviderGrammar(scopes.ToImmutable(), operations.ToImmutable()),
            diagnostics.ToImmutable());
    }

    private static void TryAddScope(
        INamedTypeSymbol type,
        ImmutableArray<GrammarScopeBinding>.Builder scopes,
        ImmutableArray<Diagnostic>.Builder diagnostics)
    {
        AttributeData? attribute = type.GetAttributes().FirstOrDefault(static candidate =>
            candidate.AttributeClass?.OriginalDefinition.GetFullMetadataName() == ScopeAttributeName);
        if (attribute is null)
        {
            return;
        }

        if (attribute.ConstructorArguments is not [{ Value: { } rawKind }, { Value: INamedTypeSymbol intrinsicsType }])
        {
            diagnostics.Add(Diagnostic.Create(
                ProcedureGenerationDiagnostics.MalformedGrammar,
                type.Locations.FirstOrDefault(),
                $"Type '{type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}' has a malformed {nameof(ProcedureGrammarScopeAttribute)}."));
            return;
        }

        GrammarScopeKind kind = (GrammarScopeKind)Convert.ToInt32(rawKind);
        string? initializerName = GetNamedString(attribute, nameof(ProcedureGrammarScopeAttribute.Initializer));
        string? finalizerName = GetNamedString(attribute, nameof(ProcedureGrammarScopeAttribute.Finalizer));
        IMethodSymbol? initializer = FindStaticMethod(intrinsicsType, initializerName);
        IMethodSymbol? finalizer = FindStaticMethod(intrinsicsType, finalizerName);

        if (initializerName is not null && initializer is null)
        {
            diagnostics.Add(Diagnostic.Create(
                ProcedureGenerationDiagnostics.MissingInitializerOrFinalizer,
                type.Locations.FirstOrDefault(),
                kind,
                type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                initializerName));
        }

        if (finalizerName is not null && finalizer is null)
        {
            diagnostics.Add(Diagnostic.Create(
                ProcedureGenerationDiagnostics.MissingInitializerOrFinalizer,
                type.Locations.FirstOrDefault(),
                kind,
                type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                finalizerName));
        }

        scopes.Add(new GrammarScopeBinding(type.OriginalDefinition, kind, intrinsicsType, initializer, finalizer));
    }

    private static void TryAddOperation(
        IMethodSymbol method,
        ImmutableDictionary<string, GrammarOperation>.Builder operations,
        ImmutableArray<Diagnostic>.Builder diagnostics)
    {
        StructuralRole? role = null;
        IMethodSymbol? intrinsic = null;
        bool isComposite = false;

        foreach (AttributeData attribute in method.GetAttributes())
        {
            string? metadataName = attribute.AttributeClass?.OriginalDefinition.GetFullMetadataName();
            if (metadataName == StructuralAttributeName)
            {
                if (attribute.ConstructorArguments is not [{ Value: { } rawRole }])
                {
                    diagnostics.Add(Diagnostic.Create(
                        ProcedureGenerationDiagnostics.MalformedGrammar,
                        method.Locations.FirstOrDefault(),
                        $"Method '{method.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}' has a malformed {nameof(StructuralOperationAttribute)}."));
                    continue;
                }

                role = (StructuralRole)Convert.ToInt32(rawRole);
            }
            else if (metadataName == TerminalAttributeName)
            {
                if (attribute.ConstructorArguments is not [{ Value: INamedTypeSymbol intrinsicsType }, { Value: string memberName }])
                {
                    diagnostics.Add(Diagnostic.Create(
                        ProcedureGenerationDiagnostics.MalformedGrammar,
                        method.Locations.FirstOrDefault(),
                        $"Method '{method.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}' has a malformed {nameof(TerminalIntrinsicAttribute)}."));
                    continue;
                }

                intrinsic = FindStaticMethod(intrinsicsType, memberName);
                if (intrinsic is null)
                {
                    diagnostics.Add(Diagnostic.Create(
                        ProcedureGenerationDiagnostics.MalformedGrammar,
                        method.Locations.FirstOrDefault(),
                        $"Intrinsic '{intrinsicsType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{memberName}' referenced by '{method.Name}' could not be resolved."));
                }
            }
            else if (metadataName == CompositeAttributeName)
            {
                isComposite = true;
            }
        }

        if (role is null && intrinsic is null && !isComposite)
        {
            return;
        }

        string key = ProviderGrammar.MethodKey(method);
        if (operations.ContainsKey(key))
        {
            diagnostics.Add(Diagnostic.Create(
                ProcedureGenerationDiagnostics.MalformedGrammar,
                method.Locations.FirstOrDefault(),
                $"Method '{method.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}' contributes more than one grammar operation."));
            return;
        }

        operations.Add(key, new GrammarOperation(method.OriginalDefinition, role, intrinsic, isComposite));
    }

    private static IMethodSymbol? FindStaticMethod(INamedTypeSymbol type, string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        return type.GetMembers(name!)
            .OfType<IMethodSymbol>()
            .FirstOrDefault(static method => method.IsStatic);
    }

    private static string? GetNamedString(AttributeData attribute, string name) =>
        attribute.NamedArguments.TryGetValue(name, out TypedConstant value) && value.Value is string text
            ? text
            : null;
}

internal sealed record ProviderGrammarDiscovery(ProviderGrammar Grammar, ImmutableArray<Diagnostic> Diagnostics);
