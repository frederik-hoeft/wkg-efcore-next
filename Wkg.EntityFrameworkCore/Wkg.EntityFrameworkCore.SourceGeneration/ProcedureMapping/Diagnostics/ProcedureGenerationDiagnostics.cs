using Microsoft.CodeAnalysis;

namespace Wkg.EntityFrameworkCore.SourceGeneration.ProcedureMapping.Diagnostics;

internal static class ProcedureGenerationDiagnostics
{
    public const string CATEGORY = "ProcedureGeneration";

    public static DiagnosticDescriptor UnsupportedSyntax { get; } = Create(
        id: "WKGLIBEFC011",
        title: "Unsupported syntax in stored-procedure Configure method",
        messageFormat: "Stored-procedure Configure methods must be a declarative fluent configuration. {0}",
        severity: DiagnosticSeverity.Error);

    public static DiagnosticDescriptor NonConstantArgument { get; } = Create(
        id: "WKGLIBEFC012",
        title: "Configuration argument is not a compile-time constant",
        messageFormat: "Argument '{0}' of '{1}' must be a compile-time constant, a property selector, or a supported conversion expression.",
        severity: DiagnosticSeverity.Error);

    public static DiagnosticDescriptor UnknownInvocation { get; } = Create(
        id: "WKGLIBEFC013",
        title: "Unknown stored-procedure configuration operation",
        messageFormat: "Invocation '{0}' is not a Core structural operation or a provider-grammar terminal.",
        severity: DiagnosticSeverity.Error);

    public static DiagnosticDescriptor MissingProcedureName { get; } = Create(
        id: "WKGLIBEFC014",
        title: "Stored procedure name is missing",
        messageFormat: "Procedure '{0}' does not call ToDatabaseProcedure or ToDatabaseFunction with a name.",
        severity: DiagnosticSeverity.Error);

    public static DiagnosticDescriptor InvalidPropertySelector { get; } = Create(
        id: "WKGLIBEFC015",
        title: "Invalid property selector",
        messageFormat: "Expression '{0}' must be a simple property selector of the form 'x => x.Property'.",
        severity: DiagnosticSeverity.Error);

    public static DiagnosticDescriptor MissingGrammar { get; } = Create(
        id: "WKGLIBEFC016",
        title: "Provider grammar is missing for a required builder scope",
        messageFormat: "Builder type '{0}' has no discoverable provider grammar for scope '{1}'.",
        severity: DiagnosticSeverity.Error);

    public static DiagnosticDescriptor MalformedGrammar { get; } = Create(
        id: "WKGLIBEFC017",
        title: "Malformed or unresolvable provider grammar",
        messageFormat: "{0}",
        severity: DiagnosticSeverity.Error);

    public static DiagnosticDescriptor MissingResultConstructor { get; } = Create(
        id: "WKGLIBEFC018",
        title: "Result type has no constructor matching mapped columns",
        messageFormat: "Result type '{0}' has no constructor whose parameters match the mapped columns by name and type.",
        severity: DiagnosticSeverity.Error);

    public static DiagnosticDescriptor InvalidConversion { get; } = Create(
        id: "WKGLIBEFC019",
        title: "Unsupported conversion expression",
        messageFormat: "Conversion expression '{0}' must be expression-bodied and must not capture local state or use control flow.",
        severity: DiagnosticSeverity.Error);

    public static DiagnosticDescriptor ContractFailure { get; } = Create(
        id: "WKGLIBEFC020",
        title: "Procedure generation source-generation contract is invalid",
        messageFormat: "{0}",
        severity: DiagnosticSeverity.Error);

    public static DiagnosticDescriptor InvalidTopology { get; } = Create(
        id: "WKGLIBEFC021",
        title: "Invalid stored-procedure topology",
        messageFormat: "{0}",
        severity: DiagnosticSeverity.Error);

    public static DiagnosticDescriptor MultipleReturnValues { get; } = Create(
        id: "WKGLIBEFC022",
        title: "Multiple ReturnValue parameters",
        messageFormat: "Procedure '{0}' cannot declare more than one ReturnValue parameter.",
        severity: DiagnosticSeverity.Error);

    public static DiagnosticDescriptor UnwritableOutput { get; } = Create(
        id: "WKGLIBEFC023",
        title: "Output member cannot be written",
        messageFormat: "I/O container member '{0}' is mapped as output but has no setter that generated code can target.",
        severity: DiagnosticSeverity.Error);

    public static DiagnosticDescriptor UnsupportedNestedSyntax { get; } = Create(
        id: "WKGLIBEFC024",
        title: "Unsupported syntax in nested builder lambda",
        messageFormat: "Nested builder configuration must be a declarative fluent subtree. {0}",
        severity: DiagnosticSeverity.Error);

    public static DiagnosticDescriptor MissingInitializerOrFinalizer { get; } = Create(
        id: "WKGLIBEFC025",
        title: "Provider grammar scope is missing a required intrinsic",
        messageFormat: "Grammar scope '{0}' on '{1}' is missing required intrinsic '{2}'.",
        severity: DiagnosticSeverity.Error);

    private static DiagnosticDescriptor Create(string id, string title, string messageFormat, DiagnosticSeverity severity) => new(
        id,
        title,
        messageFormat,
        CATEGORY,
        severity,
        isEnabledByDefault: true);
}
