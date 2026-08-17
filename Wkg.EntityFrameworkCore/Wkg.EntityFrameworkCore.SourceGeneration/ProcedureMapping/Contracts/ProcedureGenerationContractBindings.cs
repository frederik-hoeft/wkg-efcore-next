using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using Wkg.EntityFrameworkCore.SourceGeneration.Contracts;
using Wkg.EntityFrameworkCore.SourceGeneration.ProcedureMapping.Diagnostics;

namespace Wkg.EntityFrameworkCore.SourceGeneration.ProcedureMapping.Contracts;

internal sealed record ProcedureGenerationContractBindings(
    INamedTypeSymbol ExecutionPlan,
    INamedTypeSymbol CompiledProcedure,
    INamedTypeSymbol ProcedureRegistry,
    INamedTypeSymbol PlanExecutionContext,
    INamedTypeSymbol GeneratedProcedurePlanAttribute,
    INamedTypeSymbol DiscoverableProcedureConfiguration,
    INamedTypeSymbol ProcedurePlanLoader,
    INamedTypeSymbol StoredProcedure)
{
    public static ProcedureGenerationContractResolution Resolve(Compilation compilation)
    {
        CompileTimeContractResolution<ProcedureGenerationContract> raw = CompileTimeContractResolver.Resolve<ProcedureGenerationContract>(compilation);
        ImmutableArray<Diagnostic>.Builder diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

        foreach (MalformedContractRegistration malformed in raw.MalformedRegistrations)
        {
            diagnostics.Add(Diagnostic.Create(
                ProcedureGenerationDiagnostics.ContractFailure,
                malformed.Provider.Locations.FirstOrDefault(),
                $"Type '{malformed.Provider.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}' contains a malformed procedure-generation contract registration."));
        }

        foreach (DuplicateContractRegistration<ProcedureGenerationContract> duplicate in raw.Duplicates)
        {
            diagnostics.Add(Diagnostic.Create(
                ProcedureGenerationDiagnostics.ContractFailure,
                duplicate.Providers[0].Locations.FirstOrDefault(),
                $"Contract '{duplicate.Contract}' is registered by multiple types: {string.Join(", ", duplicate.Providers.Select(static provider => provider.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))}."));
        }

        Dictionary<ProcedureGenerationContract, INamedTypeSymbol> contracts = raw.Contracts.ToDictionary(static pair => pair.Key, static pair => pair.Value);
        if (raw.RegistrationCount is 0)
        {
            diagnostics.Add(Diagnostic.Create(
                ProcedureGenerationDiagnostics.ContractFailure,
                Location.None,
                "Required procedure-generation contracts are not registered by the referenced runtime assemblies."));
        }
        else
        {
            foreach (ProcedureGenerationContract contract in Enum.GetValues(typeof(ProcedureGenerationContract)).Cast<ProcedureGenerationContract>())
            {
                if (!contracts.ContainsKey(contract) && !raw.Duplicates.Any(duplicate => EqualityComparer<ProcedureGenerationContract>.Default.Equals(duplicate.Contract, contract)))
                {
                    diagnostics.Add(Diagnostic.Create(
                        ProcedureGenerationDiagnostics.ContractFailure,
                        Location.None,
                        $"Required procedure-generation contract '{contract}' is not registered by the referenced runtime assemblies."));
                }
            }
        }

        ValidateShape(contracts, ProcedureGenerationContract.ExecutionPlan, TypeKind.Interface, 0, diagnostics);
        ValidateShape(contracts, ProcedureGenerationContract.CompiledProcedure, TypeKind.Interface, 0, diagnostics);
        ValidateShape(contracts, ProcedureGenerationContract.ProcedureRegistry, TypeKind.Class, 0, diagnostics);
        ValidateShape(contracts, ProcedureGenerationContract.PlanExecutionContext, TypeKind.Class, 0, diagnostics);
        ValidateShape(contracts, ProcedureGenerationContract.GeneratedProcedurePlanAttribute, TypeKind.Class, 0, diagnostics);
        ValidateShape(contracts, ProcedureGenerationContract.DiscoverableProcedureConfiguration, TypeKind.Interface, 0, diagnostics);
        ValidateShape(contracts, ProcedureGenerationContract.ProcedurePlanLoader, TypeKind.Interface, 0, diagnostics);
        ValidateShape(contracts, ProcedureGenerationContract.StoredProcedure, TypeKind.Class, 0, diagnostics);

        if (diagnostics.Count > 0)
        {
            return new ProcedureGenerationContractResolution(null, diagnostics.ToImmutable());
        }

        return new ProcedureGenerationContractResolution(
            new ProcedureGenerationContractBindings(
                contracts[ProcedureGenerationContract.ExecutionPlan],
                contracts[ProcedureGenerationContract.CompiledProcedure],
                contracts[ProcedureGenerationContract.ProcedureRegistry],
                contracts[ProcedureGenerationContract.PlanExecutionContext],
                contracts[ProcedureGenerationContract.GeneratedProcedurePlanAttribute],
                contracts[ProcedureGenerationContract.DiscoverableProcedureConfiguration],
                contracts[ProcedureGenerationContract.ProcedurePlanLoader],
                contracts[ProcedureGenerationContract.StoredProcedure]),
            diagnostics.ToImmutable());
    }

    private static void ValidateShape(
        IReadOnlyDictionary<ProcedureGenerationContract, INamedTypeSymbol> contracts,
        ProcedureGenerationContract contract,
        TypeKind expectedKind,
        int expectedArity,
        ImmutableArray<Diagnostic>.Builder diagnostics)
    {
        if (!contracts.TryGetValue(contract, out INamedTypeSymbol? symbol)
            || symbol.TypeKind == expectedKind && symbol.Arity == expectedArity)
        {
            return;
        }

        diagnostics.Add(Diagnostic.Create(
            ProcedureGenerationDiagnostics.ContractFailure,
            symbol.Locations.FirstOrDefault(),
            $"Contract '{contract}' resolved to '{symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}', but expected a {expectedKind} with generic arity {expectedArity}."));
    }
}

internal sealed record ProcedureGenerationContractResolution(
    ProcedureGenerationContractBindings? Bindings,
    ImmutableArray<Diagnostic> Diagnostics);
