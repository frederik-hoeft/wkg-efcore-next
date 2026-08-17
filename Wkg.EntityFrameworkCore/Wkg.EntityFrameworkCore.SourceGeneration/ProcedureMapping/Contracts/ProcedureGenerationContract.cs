namespace Wkg.EntityFrameworkCore.SourceGeneration.ProcedureMapping.Contracts;

/// <summary>
/// Stable semantic roles required by the stored-procedure plan generator.
/// </summary>
internal enum ProcedureGenerationContract
{
    ExecutionPlan = 1,
    CompiledProcedure = 2,
    ProcedureRegistry = 3,
    PlanExecutionContext = 4,
    GeneratedProcedurePlanAttribute = 5,
    DiscoverableProcedureConfiguration = 6,
    ProcedurePlanLoader = 7,
    StoredProcedure = 8
}
