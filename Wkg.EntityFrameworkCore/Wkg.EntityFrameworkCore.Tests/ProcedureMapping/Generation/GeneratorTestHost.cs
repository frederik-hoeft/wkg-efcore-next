using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using System.Reflection;
using Wkg.EntityFrameworkCore.SourceGeneration.ProcedureMapping;
using Wkg.EntityFrameworkCore.Tests.Provider.Builder;

namespace Wkg.EntityFrameworkCore.Tests.ProcedureMapping.Generation;

internal static class GeneratorTestHost
{
    private static readonly CSharpParseOptions s_parseOptions = new(LanguageVersion.CSharp14);

    public static GeneratorDriverRunResult Run(string source)
    {
        CSharpCompilation compilation = CreateCompilation(source);
        ProcedurePlanGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create([generator.AsSourceGenerator()], parseOptions: s_parseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);
        return driver.GetRunResult();
    }

    public static CSharpCompilation CreateCompilation(string source)
    {
        List<MetadataReference> references = [];
        string? trusted = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
        if (trusted is not null)
        {
            foreach (string path in trusted.Split(Path.PathSeparator))
            {
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                {
                    references.Add(MetadataReference.CreateFromFile(path));
                }
            }
        }

        references.Add(MetadataReference.CreateFromFile(typeof(Wkg.EntityFrameworkCore.ProcedureMapping.Generation.IProcedureExecutionPlan).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(TestProcedureBuilder<,>).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(Microsoft.EntityFrameworkCore.DbContext).Assembly.Location));

        return CSharpCompilation.Create(
            assemblyName: "GeneratorTestAssembly",
            syntaxTrees: [CSharpSyntaxTree.ParseText(source, s_parseOptions)],
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
    }

    public static ImmutableArray<Diagnostic> GeneratorDiagnostics(this GeneratorDriverRunResult result) =>
        [.. result.Results.SelectMany(static run => run.Diagnostics)];

    public static string CombinedGeneratedSource(this GeneratorDriverRunResult result) =>
        string.Join(Environment.NewLine, result.Results.SelectMany(static run => run.GeneratedSources).Select(static source => source.SourceText.ToString()));
}
