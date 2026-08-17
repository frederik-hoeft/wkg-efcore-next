using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Wkg.EntityFrameworkCore.ProcedureMapping.Generation;
using Wkg.EntityFrameworkCore.ProcedureMapping.ResultCollections;
using Wkg.EntityFrameworkCore.SourceGeneration.Contracts;
using Wkg.EntityFrameworkCore.SourceGeneration.ProcedureMapping.Contracts;

namespace Wkg.EntityFrameworkCore.ProcedureMapping.Runtime;

/// <summary>
/// Provider-agnostic execution context that orchestrates ADO.NET calls for an <see cref="IProcedureExecutionPlan"/>.
/// </summary>
[GeneratorContractRegistration<ProcedureGenerationContract>(ProcedureGenerationContract.PlanExecutionContext)]
public sealed class PlanExecutionContext : IProcedureExecutionContext
{
    private readonly IProcedureExecutionPlan _plan;
    private readonly DbParameter?[] _parameters;

    /// <summary>
    /// Creates a new stateful execution context for <paramref name="plan"/>.
    /// </summary>
    public PlanExecutionContext(IProcedureExecutionPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        _plan = plan;
        _parameters = plan.ParameterCount > 0
            ? new DbParameter?[plan.ParameterCount]
            : [];
    }

    /// <inheritdoc/>
    public void Execute(DatabaseFacade dbContext, object container)
    {
        _plan.BindParameters(_parameters, container);
        object? returnValue = ExecuteProcedureScalar(dbContext);
        _plan.StoreOutputs(_parameters, container, returnValue);
    }

    /// <inheritdoc/>
    public async Task ExecuteAsync(DatabaseFacade dbContext, object container, CancellationToken cancellationToken = default)
    {
        _plan.BindParameters(_parameters, container);
        object? returnValue = await ExecuteProcedureScalarAsync(dbContext, cancellationToken).ConfigureAwait(false);
        _plan.StoreOutputs(_parameters, container, returnValue);
    }

    /// <inheritdoc/>
    public IResultContainer<TResult> Execute<TResult>(DatabaseFacade dbContext, object container) where TResult : class
    {
        _plan.BindParameters(_parameters, container);
        IResultContainer<TResult> result = _plan.IsCollectionResult
            ? ExecuteProcedureReader<TResult>(dbContext)
            : ExecuteProcedureReaderSingle<TResult>(dbContext);
        _plan.StoreOutputs(_parameters, container, scalarReturn: null);
        return result;
    }

    /// <inheritdoc/>
    public async Task<IResultContainer<TResult>> ExecuteAsync<TResult>(DatabaseFacade dbContext, object container, CancellationToken cancellationToken = default) where TResult : class
    {
        _plan.BindParameters(_parameters, container);
        IResultContainer<TResult> result = _plan.IsCollectionResult
            ? await ExecuteProcedureReaderAsync<TResult>(dbContext, cancellationToken).ConfigureAwait(false)
            : await ExecuteProcedureReaderSingleAsync<TResult>(dbContext, cancellationToken).ConfigureAwait(false);
        _plan.StoreOutputs(_parameters, container, scalarReturn: null);
        return result;
    }

    [SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Stored procedure name is runtime-immutable and not influenced by user input.")]
    private object? ExecuteProcedureScalar(DatabaseFacade databaseFacade)
    {
        DbConnection connection = databaseFacade.GetDbConnection();
        if (connection.State is ConnectionState.Closed)
        {
            connection.Open();
        }
        using DbCommand cmd = CreateCommand(databaseFacade, connection, includeReturnValue: false);
        return cmd.ExecuteScalar();
    }

    [SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Stored procedure name is runtime-immutable and not influenced by user input.")]
    private async Task<object?> ExecuteProcedureScalarAsync(DatabaseFacade databaseFacade, CancellationToken cancellationToken)
    {
        DbConnection connection = databaseFacade.GetDbConnection();
        if (connection.State is ConnectionState.Closed)
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }
        await using DbCommand cmd = CreateCommand(databaseFacade, connection, includeReturnValue: false);
        return await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
    }

    [SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Stored procedure name is runtime-immutable and not influenced by user input.")]
    private ResultElement<TResult> ExecuteProcedureReaderSingle<TResult>(DatabaseFacade databaseFacade)
        where TResult : class
    {
        DbConnection connection = databaseFacade.GetDbConnection();
        if (connection.State is ConnectionState.Closed)
        {
            connection.Open();
        }
        using DbCommand cmd = CreateCommand(databaseFacade, connection, includeReturnValue: true);
        using DbDataReader reader = cmd.ExecuteReader();
        TResult? result = null;
        if (reader.Read())
        {
            result = Unsafe.As<TResult>(_plan.ReadResult(reader));
        }
        return new ResultElement<TResult>(result);
    }

    [SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Stored procedure name is runtime-immutable and not influenced by user input.")]
    private async Task<ResultElement<TResult>> ExecuteProcedureReaderSingleAsync<TResult>(DatabaseFacade databaseFacade, CancellationToken cancellationToken)
        where TResult : class
    {
        DbConnection connection = databaseFacade.GetDbConnection();
        if (connection.State is ConnectionState.Closed)
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }
        await using DbCommand cmd = CreateCommand(databaseFacade, connection, includeReturnValue: true);
        await using DbDataReader reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        TResult? result = null;
        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            result = Unsafe.As<TResult>(_plan.ReadResult(reader));
        }
        return new ResultElement<TResult>(result);
    }

    [SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Stored procedure name is runtime-immutable and not influenced by user input.")]
    private ResultCollection<TResult> ExecuteProcedureReader<TResult>(DatabaseFacade databaseFacade)
        where TResult : class
    {
        DbConnection connection = databaseFacade.GetDbConnection();
        if (connection.State is ConnectionState.Closed)
        {
            connection.Open();
        }
        using DbCommand cmd = CreateCommand(databaseFacade, connection, includeReturnValue: true);
        using DbDataReader reader = cmd.ExecuteReader();
        List<TResult> results = [];
        while (reader.Read())
        {
            results.Add(Unsafe.As<TResult>(_plan.ReadResult(reader)));
        }
        return new ResultCollection<TResult>(results);
    }

    [SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Stored procedure name is runtime-immutable and not influenced by user input.")]
    private async Task<ResultCollection<TResult>> ExecuteProcedureReaderAsync<TResult>(DatabaseFacade databaseFacade, CancellationToken cancellationToken)
        where TResult : class
    {
        DbConnection connection = databaseFacade.GetDbConnection();
        if (connection.State is ConnectionState.Closed)
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }
        await using DbCommand cmd = CreateCommand(databaseFacade, connection, includeReturnValue: true);
        await using DbDataReader reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        List<TResult> results = [];
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            results.Add(Unsafe.As<TResult>(_plan.ReadResult(reader)));
        }
        return new ResultCollection<TResult>(results);
    }

    [SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Stored procedure name is runtime-immutable and not influenced by user input.")]
    private DbCommand CreateCommand(DatabaseFacade databaseFacade, DbConnection connection, bool includeReturnValue)
    {
        DbCommand cmd = connection.CreateCommand();
        cmd.Transaction = databaseFacade.CurrentTransaction?.GetDbTransaction();
        cmd.CommandText = _plan.ProcedureName;
        cmd.CommandType = CommandType.StoredProcedure;
        foreach (DbParameter? parameter in _parameters)
        {
            if (includeReturnValue || _plan.IsFunction || parameter!.Direction is not ParameterDirection.ReturnValue)
            {
                cmd.Parameters.Add(parameter!);
            }
        }
        return cmd;
    }
}
