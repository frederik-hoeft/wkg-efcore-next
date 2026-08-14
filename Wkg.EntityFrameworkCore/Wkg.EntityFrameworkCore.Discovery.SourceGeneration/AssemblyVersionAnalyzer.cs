using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Wkg.EntityFrameworkCore.Discovery.SourceGeneration;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AssemblyVersionAnalyzer : DiagnosticAnalyzer
{
    private const string SOURCE_GENERATOR_ASSEMBLY_NAME = "Wkg.EntityFrameworkCore.Discovery.SourceGeneration";
    private const string DEPENDENT_ASSEMBLY_NAME = "Wkg.EntityFrameworkCore";

    private static readonly DiagnosticDescriptor s_missingDependency = new(
        id: "WKGLIBEFC001",
        title: "Missing dependent assembly",
        messageFormat: $"The source generator '{SOURCE_GENERATOR_ASSEMBLY_NAME}' requires the dependent assembly '{DEPENDENT_ASSEMBLY_NAME}' to be referenced in the project to ensure compatibility",
        category: "Compatibility",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: $"Ensures that the source generator '{SOURCE_GENERATOR_ASSEMBLY_NAME}' has access to the dependent assembly '{DEPENDENT_ASSEMBLY_NAME}' to prevent code generation issues due to missing dependencies.",
        customTags: ["CompilationEnd"]);

    private static readonly DiagnosticDescriptor s_incompatibleVersion = new(
        id: "WKGLIBEFC002",
        title: "Incompatible assembly version",
        messageFormat: $"Source generator '{SOURCE_GENERATOR_ASSEMBLY_NAME}' must have the same version as '{DEPENDENT_ASSEMBLY_NAME}' to ensure compatibility. '{SOURCE_GENERATOR_ASSEMBLY_NAME}' has version '{{0}}', but expected version was '{{1}}' from '{DEPENDENT_ASSEMBLY_NAME}'.",
        category: "Compatibility",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: $"Ensures that the source generator '{SOURCE_GENERATOR_ASSEMBLY_NAME}' and the dependent assembly '{DEPENDENT_ASSEMBLY_NAME}' have matching versions to prevent code generation issues due to API mismatches.",
        customTags: ["CompilationEnd"]);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => 
    [
        s_missingDependency,
        s_incompatibleVersion
    ];

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationAction(compilationContext =>
        {
            IAssemblySymbol? wkgEfCore = compilationContext.Compilation.References
                .Select(compilationContext.Compilation.GetAssemblyOrModuleSymbol)
                .Distinct(SymbolEqualityComparer.Default)
                .OfType<IAssemblySymbol>()
                .FirstOrDefault(assembly => assembly.Name.Equals(DEPENDENT_ASSEMBLY_NAME, StringComparison.Ordinal));
            if (wkgEfCore is not { Identity.Version: { } wkgEfCoreVersion })
            {
                Diagnostic diagnostic = Diagnostic.Create(s_missingDependency, Location.None);
                compilationContext.ReportDiagnostic(diagnostic);
                return;
            }
            Version analyzerVersion = WkgEntityFrameworkCoreDiscoverySourceGeneration.VersionInfo.Version;
            // breaking changes are only supported for major and minor version changes
            if (wkgEfCoreVersion.Major != analyzerVersion.Major || wkgEfCoreVersion.Minor != analyzerVersion.Minor)
            {
                Diagnostic diagnostic = Diagnostic.Create(
                    s_incompatibleVersion,
                    Location.None,
                    WkgEntityFrameworkCoreDiscoverySourceGeneration.VersionInfo.Version.ToString(),
                    wkgEfCoreVersion.ToString());
                compilationContext.ReportDiagnostic(diagnostic);
            }
        });
    }
}
