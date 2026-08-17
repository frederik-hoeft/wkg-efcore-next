using Wkg.EntityFrameworkCore.SourceGeneration.Contracts;
using Wkg.EntityFrameworkCore.SourceGeneration.ProcedureMapping.Contracts;

namespace Wkg.EntityFrameworkCore.ProcedureMapping.Configuration;

/// <summary>
/// Marks a stored-procedure command object for compile-time discovery and plan registration.
/// </summary>
/// <remarks>
/// Provider-specific <c>IReflectiveProcedureConfiguration&lt;TProcedure, TIOContainer&gt;</c> interfaces inherit this marker.
/// The source generator discovers implementing types and registers their generated execution plans.
/// </remarks>
[GeneratorContractRegistration<ProcedureGenerationContract>(ProcedureGenerationContract.DiscoverableProcedureConfiguration)]
public interface IDiscoverableProcedureConfiguration;
