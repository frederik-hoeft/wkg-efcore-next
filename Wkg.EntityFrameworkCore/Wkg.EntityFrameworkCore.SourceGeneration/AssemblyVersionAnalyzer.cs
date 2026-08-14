using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Wkg.EntityFrameworkCore.SourceGeneration;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AssemblyVersionAnalyzer : DiagnosticAnalyzer
{
    private const string SOURCE_GENERATOR_ASSEMBLY_NAME = "Wkg.EntityFrameworkCore.SourceGeneration";
    private const string DEPENDENT_ASSEMBLY_NAME = "Wkg.EntityFrameworkCore";

    private static readonly DiagnosticDescriptor s_missingDependency = new(
        id: "WKGLIBEFC010",
        title: "Missing dependent assembly",
        messageFormat: $"The source-generator package '{SOURCE_GENERATOR_ASSEMBLY_NAME}' requires '{DEPENDENT_ASSEMBLY_NAME}' to be present in the compilation",
        category: "Compatibility",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: $"Ensures that '{SOURCE_GENERATOR_ASSEMBLY_NAME}' can resolve the runtime contracts supplied by '{DEPENDENT_ASSEMBLY_NAME}'.",
        customTags: ["CompilationEnd"]);

    private static readonly DiagnosticDescriptor s_incompatibleVersion = new(
        id: "WKGLIBEFC001",
        title: "Incompatible assembly version",
        messageFormat: $"Source-generator package '{SOURCE_GENERATOR_ASSEMBLY_NAME}' must have the same major and minor version as '{DEPENDENT_ASSEMBLY_NAME}'. '{SOURCE_GENERATOR_ASSEMBLY_NAME}' has version '{{0}}', but expected version was '{{1}}' from '{DEPENDENT_ASSEMBLY_NAME}'.",
        category: "Compatibility",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: $"Ensures that '{SOURCE_GENERATOR_ASSEMBLY_NAME}' and '{DEPENDENT_ASSEMBLY_NAME}' use compatible compile-time contracts.",
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
        context.RegisterCompilationAction(static compilationContext =>
        {
            Compilation compilation = compilationContext.Compilation;
            if (compilation.Assembly.Name.Equals(DEPENDENT_ASSEMBLY_NAME, StringComparison.Ordinal))
            {
                // The runtime project references this assembly as an analyzer solely to bootstrap
                // and persist compile-time contract registrations. Consumer compatibility is
                // validated when the runtime is referenced by a downstream compilation.
                return;
            }

            IAssemblySymbol? wkgEfCore = compilation.References
                .Select(compilation.GetAssemblyOrModuleSymbol)
                .OfType<IAssemblySymbol>()
                .FirstOrDefault(static assembly => assembly.Name.Equals(DEPENDENT_ASSEMBLY_NAME, StringComparison.Ordinal));
            if (wkgEfCore is not { Identity.Version: { } wkgEfCoreVersion })
            {
                compilationContext.ReportDiagnostic(Diagnostic.Create(s_missingDependency, Location.None));
                return;
            }

            Version analyzerVersion = WkgEntityFrameworkCoreSourceGeneration.VersionInfo.Version;
            if (wkgEfCoreVersion.Major != analyzerVersion.Major || wkgEfCoreVersion.Minor != analyzerVersion.Minor)
            {
                compilationContext.ReportDiagnostic(Diagnostic.Create(
                    s_incompatibleVersion,
                    Location.None,
                    analyzerVersion.ToString(),
                    wkgEfCoreVersion.ToString()));
            }
        });
    }
}
