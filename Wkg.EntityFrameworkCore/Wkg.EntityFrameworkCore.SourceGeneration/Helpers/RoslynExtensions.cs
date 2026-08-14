using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Text;

namespace Wkg.EntityFrameworkCore.SourceGeneration.Helpers;

/// <summary>
/// Quality-of-life extension methods for Roslyn symbols and compilations.
/// </summary>
internal static class RoslynExtensions
{
    public static IEnumerable<IAssemblySymbol> GetAllAssemblies(this Compilation compilation)
    {
        HashSet<IAssemblySymbol> seen = new(SymbolEqualityComparer.Default);
        if (seen.Add(compilation.Assembly))
        {
            yield return compilation.Assembly;
        }

        foreach (MetadataReference reference in compilation.References)
        {
            if (compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assembly && seen.Add(assembly))
            {
                yield return assembly;
            }
        }
    }

    public static IEnumerable<INamedTypeSymbol> GetAllTypes(this INamespaceSymbol namespaceSymbol)
    {
        Stack<INamespaceSymbol> remaining = [];
        remaining.Push(namespaceSymbol);
        while (remaining.Count > 0)
        {
            INamespaceSymbol currentNamespace = remaining.Pop();
            foreach (INamedTypeSymbol type in currentNamespace.GetTypeMembers())
            {
                foreach (INamedTypeSymbol nestedType in type.GetTypeAndNestedTypes())
                {
                    yield return nestedType;
                }
            }
            foreach (INamespaceSymbol nestedNamespace in currentNamespace.GetNamespaceMembers())
            {
                remaining.Push(nestedNamespace);
            }
        }
    }

    public static string GetFullMetadataName(this INamedTypeSymbol type)
    {
        StringBuilder builder = new();
        AppendMetadataName(builder, type);
        return builder.ToString();
    }

    public static bool IsConstructedFrom(this INamedTypeSymbol type, INamedTypeSymbol genericDefinition) =>
        type.IsGenericType && SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, genericDefinition);

    public static bool TryGetValue(this ImmutableArray<KeyValuePair<string, TypedConstant>> source, string key, out TypedConstant value)
    {
        foreach (KeyValuePair<string, TypedConstant> pair in source)
        {
            if (pair.Key == key)
            {
                value = pair.Value;
                return true;
            }
        }
        value = default;
        return false;
    }

    private static IEnumerable<INamedTypeSymbol> GetTypeAndNestedTypes(this INamedTypeSymbol type)
    {
        yield return type;
        foreach (INamedTypeSymbol nestedType in type.GetTypeMembers())
        {
            foreach (INamedTypeSymbol candidate in nestedType.GetTypeAndNestedTypes())
            {
                yield return candidate;
            }
        }
    }

    private static void AppendMetadataName(StringBuilder builder, INamedTypeSymbol type)
    {
        if (type.ContainingType is not null)
        {
            AppendMetadataName(builder, type.ContainingType);
            builder.Append('+');
        }
        else if (!type.ContainingNamespace.IsGlobalNamespace)
        {
            builder.Append(type.ContainingNamespace.ToDisplayString());
            builder.Append('.');
        }

        builder.Append(type.MetadataName);
    }
}
