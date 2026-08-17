using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using Wkg.EntityFrameworkCore.ProcedureMapping.Generation;

namespace Wkg.EntityFrameworkCore.SourceGeneration.ProcedureMapping.Grammar;

internal sealed record ProviderGrammar(
    ImmutableArray<GrammarScopeBinding> Scopes,
    ImmutableDictionary<string, GrammarOperation> Operations)
{
    public bool TryGetOperation(IMethodSymbol method, out GrammarOperation operation)
    {
        IMethodSymbol? current = method;
        while (current is not null)
        {
            if (Operations.TryGetValue(MethodKey(current), out operation))
            {
                return true;
            }

            if (current.ReducedFrom is { } reduced && Operations.TryGetValue(MethodKey(reduced), out operation))
            {
                return true;
            }

            current = current.OverriddenMethod;
        }

        if (method.ExplicitInterfaceImplementations is { Length: > 0 } implementations)
        {
            foreach (IMethodSymbol implementation in implementations)
            {
                if (Operations.TryGetValue(MethodKey(implementation), out operation))
                {
                    return true;
                }
            }
        }

        operation = null!;
        return false;
    }

    public GrammarScopeBinding? FindScope(ITypeSymbol builderType, GrammarScopeKind kind)
    {
        ITypeSymbol? current = builderType;
        while (current is not null)
        {
            foreach (GrammarScopeBinding scope in Scopes)
            {
                if (scope.Kind == kind && IsSameOriginal(current, scope.BuilderType))
                {
                    return scope;
                }
            }

            current = current.BaseType;
        }

        foreach (INamedTypeSymbol iface in AllInterfaces(builderType))
        {
            foreach (GrammarScopeBinding scope in Scopes)
            {
                if (scope.Kind == kind && IsSameOriginal(iface, scope.BuilderType))
                {
                    return scope;
                }
            }
        }

        return null;
    }

    public GrammarScopeBinding? FindScopeForBuilder(ITypeSymbol builderType)
    {
        ITypeSymbol? current = builderType;
        while (current is not null)
        {
            foreach (GrammarScopeBinding scope in Scopes)
            {
                if (IsSameOriginal(current, scope.BuilderType))
                {
                    return scope;
                }
            }

            current = current.BaseType;
        }

        foreach (INamedTypeSymbol iface in AllInterfaces(builderType))
        {
            foreach (GrammarScopeBinding scope in Scopes)
            {
                if (IsSameOriginal(iface, scope.BuilderType))
                {
                    return scope;
                }
            }
        }

        return null;
    }

    public static string MethodKey(IMethodSymbol method)
    {
        IMethodSymbol original = method.OriginalDefinition;
        string containing = original.ContainingType.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        string parameters = string.Join(",", original.Parameters.Select(static parameter =>
            parameter.Type.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
        return $"{containing}|{original.Name}|{original.Arity}|{parameters}";
    }

    private static bool IsSameOriginal(ITypeSymbol left, ITypeSymbol right) =>
        SymbolEqualityComparer.Default.Equals(left.OriginalDefinition, right.OriginalDefinition);

    private static IEnumerable<INamedTypeSymbol> AllInterfaces(ITypeSymbol type) => type.AllInterfaces;
}

internal sealed record GrammarScopeBinding(
    INamedTypeSymbol BuilderType,
    GrammarScopeKind Kind,
    INamedTypeSymbol IntrinsicsType,
    IMethodSymbol? Initializer,
    IMethodSymbol? Finalizer);

internal sealed record GrammarOperation(
    IMethodSymbol BuilderMethod,
    StructuralRole? Role,
    IMethodSymbol? Intrinsic,
    bool IsComposite);
