using System.Data.Common;
using Wkg.EntityFrameworkCore.ProcedureMapping.Compiler.Output;
using Wkg.EntityFrameworkCore.ProcedureMapping.Runtime;
using Wkg.EntityFrameworkCore.SourceGeneration.Contracts;
using Wkg.EntityFrameworkCore.SourceGeneration.ProcedureMapping.Contracts;

namespace Wkg.EntityFrameworkCore.ProcedureMapping.Generation;

/// <summary>
/// AOT-compatible execution plan for a stored procedure.
/// </summary>
/// <remarks>
/// Generated plans implement this interface with statically bound intrinsic calls.
/// The historical IL compiler adapts <see cref="CompiledProcedure{TCompiledParameter}"/> to the same contract.
/// </remarks>
[GeneratorContractRegistration<ProcedureGenerationContract>(ProcedureGenerationContract.ExecutionPlan)]
public interface IProcedureExecutionPlan
{
    /// <summary>
    /// The CLR type of the command object that owns this plan.
    /// </summary>
    Type ProcedureType { get; }

    /// <summary>
    /// The database command text (procedure or function name, possibly provider-qualified).
    /// </summary>
    string ProcedureName { get; }

    /// <summary>
    /// Indicates whether the mapped object is a database function.
    /// </summary>
    bool IsFunction { get; }

    /// <summary>
    /// The number of ADO.NET parameters allocated for an execution.
    /// </summary>
    int ParameterCount { get; }

    /// <summary>
    /// Indicates whether the procedure returns a mapped result set.
    /// </summary>
    bool HasResult { get; }

    /// <summary>
    /// Indicates whether a mapped result set is consumed as a collection of rows.
    /// </summary>
    bool IsCollectionResult { get; }

    /// <summary>
    /// Populates <paramref name="parameters"/> from the I/O <paramref name="container"/> before execution.
    /// </summary>
    void BindParameters(DbParameter?[] parameters, object container);

    /// <summary>
    /// Writes output / return-value parameters from <paramref name="parameters"/> back into the I/O <paramref name="container"/>.
    /// </summary>
    /// <param name="parameters">The ADO.NET parameters used for the call.</param>
    /// <param name="container">The I/O container that receives output values.</param>
    /// <param name="scalarReturn">The scalar value returned by <c>ExecuteScalar</c> for non-result executions; ignored for result-set executions.</param>
    void StoreOutputs(DbParameter?[] parameters, object container, object? scalarReturn);

    /// <summary>
    /// Constructs a result entity from the current row of <paramref name="reader"/>.
    /// </summary>
    object ReadResult(DbDataReader reader);
}
