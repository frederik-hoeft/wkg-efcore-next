using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Collections.Frozen;
using Wkg.EntityFrameworkCore.ProcedureMapping.Compiler.Output;
using Wkg.EntityFrameworkCore.SourceGeneration.Contracts;
using Wkg.EntityFrameworkCore.SourceGeneration.ProcedureMapping.Contracts;

namespace Wkg.EntityFrameworkCore.ProcedureMapping.Runtime;

/// <summary>
/// Process-wide registry of compiled or source-generated stored-procedure execution plans.
/// </summary>
[GeneratorContractRegistration<ProcedureGenerationContract>(ProcedureGenerationContract.ProcedureRegistry)]
public static class ProcedureRegistry
{
    internal static FrozenDictionary<Type, ICompiledProcedure> Procedures { get; private set; } = FrozenDictionary<Type, ICompiledProcedure>.Empty;

    internal static T GetProcedure<T>(DatabaseFacade database) where T : IStoredProcedure, new()
    {
        if (Procedures.TryGetValue(typeof(T), out ICompiledProcedure? compiledProcedure))
        {
            T instance = new()
            {
                ExecutionContext = compiledProcedure.CreateExecutionContext(),
                DbContext = database
            };
            return instance;
        }

        throw new InvalidOperationException($"Procedure {typeof(T).Name} has not been mapped or built.");
    }

    /// <summary>
    /// Indicates whether a plan for <paramref name="procedureType"/> has already been registered.
    /// </summary>
    public static bool IsRegistered(Type procedureType)
    {
        ArgumentNullException.ThrowIfNull(procedureType);
        return Procedures.ContainsKey(procedureType);
    }

    /// <summary>
    /// Registers a compiled or source-generated procedure plan.
    /// </summary>
    /// <returns><see langword="true"/> if the plan was added; <see langword="false"/> if a plan for the same command object was already present.</returns>
    public static bool TryRegister(ICompiledProcedure compiledProcedure) => TryAddProcedure(compiledProcedure);

    // we expect Procedures to be read more often than being written to
    // so we use a frozen dictionary to optimize for reads with a copy-on-write strategy for writes
    // this trades off startup performance for runtime performance
    internal static bool TryAddProcedure(ICompiledProcedure compiledProcedure)
    {
        ArgumentNullException.ThrowIfNull(compiledProcedure);
        Dictionary<Type, ICompiledProcedure> procedures = new(Procedures);
        if (!procedures.TryAdd(compiledProcedure.ProcedureType, compiledProcedure))
        {
            return false;
        }
        Procedures = procedures.ToFrozenDictionary();
        return true;
    }
}
