using Wkg.EntityFrameworkCore.SourceGeneration.Contracts;
using Wkg.EntityFrameworkCore.SourceGeneration.ProcedureMapping.Contracts;

namespace Wkg.EntityFrameworkCore.ProcedureMapping.Generation;

/// <summary>
/// Registers source-generated stored-procedure execution plans with the Core procedure registry.
/// </summary>
[GeneratorContractRegistration<ProcedureGenerationContract>(ProcedureGenerationContract.ProcedurePlanLoader)]
public interface IProcedurePlanLoader
{
    /// <summary>
    /// Registers every discoverable generated plan associated with this loader.
    /// </summary>
    void LoadProcedurePlans();
}
