using Wkg.EntityFrameworkCore.SourceGeneration.Contracts;
using Wkg.EntityFrameworkCore.SourceGeneration.ProcedureMapping.Contracts;

namespace Wkg.EntityFrameworkCore.ProcedureMapping.Generation;

/// <summary>
/// Marks a source-generated stored-procedure execution plan and associates it with the command object it implements.
/// </summary>
/// <param name="procedureType">The CLR type of the stored-procedure command object.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
[GeneratorContractRegistration<ProcedureGenerationContract>(ProcedureGenerationContract.GeneratedProcedurePlanAttribute)]
public sealed class GeneratedProcedurePlanAttribute(Type procedureType) : Attribute
{
    /// <summary>
    /// The CLR type of the stored-procedure command object implemented by the annotated plan.
    /// </summary>
    public Type ProcedureType { get; } = procedureType;
}
