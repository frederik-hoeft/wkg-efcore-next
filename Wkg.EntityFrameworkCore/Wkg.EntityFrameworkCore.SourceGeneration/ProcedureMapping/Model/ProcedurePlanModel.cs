using Microsoft.CodeAnalysis;
using System.Data;
using Wkg.EntityFrameworkCore.SourceGeneration.ProcedureMapping.Grammar;

namespace Wkg.EntityFrameworkCore.SourceGeneration.ProcedureMapping.Model;

internal sealed class ProcedurePlanModel
{
    public required INamedTypeSymbol ProcedureType { get; init; }
    public required INamedTypeSymbol IOContainerType { get; init; }
    public required Location Location { get; init; }
    public string? ProcedureName { get; set; }
    public bool IsFunction { get; set; }
    public GrammarScopeBinding? ProcedureScope { get; set; }
    public List<BoundTerminal> ProcedureTerminals { get; } = [];
    public List<ParameterPlanModel> Parameters { get; } = [];
    public ResultPlanModel? Result { get; set; }
    public List<Diagnostic> Diagnostics { get; } = [];
    public bool IsDiscoverable { get; set; }
}

internal sealed class ParameterPlanModel
{
    public required IPropertySymbol Property { get; init; }
    public string Name { get; set; } = string.Empty;
    public ParameterDirection Direction { get; set; } = ParameterDirection.Input;
    public int Size { get; set; }
    public GrammarScopeBinding? Scope { get; set; }
    public List<BoundTerminal> Terminals { get; } = [];
}

internal sealed class ResultPlanModel
{
    public required INamedTypeSymbol ResultType { get; init; }
    public bool IsCollection { get; set; } = true;
    public GrammarScopeBinding? Scope { get; set; }
    public List<BoundTerminal> Terminals { get; } = [];
    public List<ColumnPlanModel> Columns { get; } = [];
}

internal sealed class ColumnPlanModel
{
    public required IPropertySymbol Property { get; init; }
    public string? Name { get; set; }
    public bool IsNullable { get; set; }
    public ConversionPlan? Conversion { get; set; }
    public GrammarScopeBinding? Scope { get; set; }
    public List<BoundTerminal> Terminals { get; } = [];
}

internal sealed record BoundTerminal(IMethodSymbol Intrinsic, IReadOnlyList<string> Arguments);

internal sealed record ConversionPlan(ITypeSymbol SourceType, string RenderedExpression);
