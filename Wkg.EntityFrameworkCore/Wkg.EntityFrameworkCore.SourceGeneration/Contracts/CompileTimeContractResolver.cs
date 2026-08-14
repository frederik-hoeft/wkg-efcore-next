using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using Wkg.EntityFrameworkCore.SourceGeneration.Helpers;

namespace Wkg.EntityFrameworkCore.SourceGeneration.Contracts;

/// <summary>
/// Resolves enum-based compile-time contract registrations from the active compilation and its references.
/// </summary>
internal static class CompileTimeContractResolver
{
    public static CompileTimeContractResolution<TContract> Resolve<TContract>(Compilation compilation)
        where TContract : struct, Enum
    {
        _ = compilation ?? throw new ArgumentNullException(nameof(compilation));

        string registrationAttributeMetadataName = typeof(GeneratorContractRegistrationAttribute<>).FullName
            ?? throw new InvalidOperationException("The generator contract registration attribute must have a full name.");
        string contractMetadataName = typeof(TContract).FullName
            ?? throw new InvalidOperationException($"The contract type '{typeof(TContract)}' must have a full name.");

        Dictionary<TContract, List<INamedTypeSymbol>> registrations = [];
        ImmutableArray<MalformedContractRegistration>.Builder malformed = ImmutableArray.CreateBuilder<MalformedContractRegistration>();
        int registrationCount = 0;

        foreach (IAssemblySymbol assembly in compilation.GetAllAssemblies())
        {
            foreach (INamedTypeSymbol type in assembly.GlobalNamespace.GetAllTypes())
            {
                foreach (AttributeData attribute in type.GetAttributes())
                {
                    if (!TryMatchContractFamily(attribute, registrationAttributeMetadataName, contractMetadataName))
                    {
                        continue;
                    }

                    ++registrationCount;
                    if (!TryReadContract(attribute, out TContract contract))
                    {
                        malformed.Add(new MalformedContractRegistration(type, attribute));
                        continue;
                    }

                    if (!registrations.TryGetValue(contract, out List<INamedTypeSymbol>? providers))
                    {
                        providers = [];
                        registrations.Add(contract, providers);
                    }
                    else
                    {
                        Debug.WriteLine($"Duplicate registration for contract '{contract}' found in type '{type.GetFullMetadataName()}'.");
                    }
                    providers.Add(type);
                }
            }
        }

        ImmutableDictionary<TContract, INamedTypeSymbol>.Builder resolved = ImmutableDictionary.CreateBuilder<TContract, INamedTypeSymbol>();
        ImmutableArray<DuplicateContractRegistration<TContract>>.Builder duplicates = ImmutableArray.CreateBuilder<DuplicateContractRegistration<TContract>>();
        foreach (KeyValuePair<TContract, List<INamedTypeSymbol>> registration in registrations.OrderBy(static pair => Convert.ToInt64(pair.Key, CultureInfo.InvariantCulture)))
        {
            List<INamedTypeSymbol> providers = registration.Value;
            providers.Sort(static (left, right) => StringComparer.Ordinal.Compare(
                GetStableSymbolIdentity(left),
                GetStableSymbolIdentity(right)));

            if (providers.Count is 1)
            {
                resolved.Add(registration.Key, providers[0]);
                continue;
            }

            duplicates.Add(new DuplicateContractRegistration<TContract>(registration.Key, [.. providers]));
        }

        return new CompileTimeContractResolution<TContract>(
            resolved.ToImmutable(),
            duplicates.ToImmutable(),
            malformed.ToImmutable(),
            registrationCount);
    }

    private static bool TryMatchContractFamily(AttributeData attribute, string registrationAttributeMetadataName, string contractMetadataName) => attribute.AttributeClass is
    {
        IsGenericType: true,
        OriginalDefinition: { } originalDefinition,
        TypeArguments: [INamedTypeSymbol contractType]
    }
    && originalDefinition.GetFullMetadataName() == registrationAttributeMetadataName
    && contractType.OriginalDefinition.GetFullMetadataName() == contractMetadataName;

    private static bool TryReadContract<TContract>(AttributeData attribute, out TContract contract)
        where TContract : struct, Enum
    {
        contract = default;
        if (attribute.ConstructorArguments is not [{ Value: { } rawValue }])
        {
            return false;
        }

        try
        {
            long numericValue = Convert.ToInt64(rawValue, CultureInfo.InvariantCulture);
            object enumValue = Enum.ToObject(typeof(TContract), numericValue);
            if (!Enum.IsDefined(typeof(TContract), enumValue))
            {
                return false;
            }
            contract = (TContract)enumValue;
            return true;
        }
        catch (Exception exception) when (exception is InvalidCastException or FormatException or OverflowException or ArgumentException)
        {
            return false;
        }
    }

    private static string GetStableSymbolIdentity(INamedTypeSymbol symbol) =>
        $"{symbol.ContainingAssembly.Identity}|{symbol.GetFullMetadataName()}";
}

internal sealed record CompileTimeContractResolution<TContract>(
    ImmutableDictionary<TContract, INamedTypeSymbol> Contracts,
    ImmutableArray<DuplicateContractRegistration<TContract>> Duplicates,
    ImmutableArray<MalformedContractRegistration> MalformedRegistrations,
    int RegistrationCount)
    where TContract : struct, Enum;

internal sealed record DuplicateContractRegistration<TContract>(TContract Contract, ImmutableArray<INamedTypeSymbol> Providers)
    where TContract : struct, Enum;

internal sealed record MalformedContractRegistration(INamedTypeSymbol Provider, AttributeData Attribute);
