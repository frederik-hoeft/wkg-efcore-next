using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;
using Wkg.EntityFrameworkCore.SourceGeneration.Discovery;
using Wkg.EntityFrameworkCore.SourceGeneration.Helpers;
using Wkg.EntityFrameworkCore.SourceGeneration.ProcedureMapping.Analysis;
using Wkg.EntityFrameworkCore.SourceGeneration.ProcedureMapping.Contracts;

namespace Wkg.EntityFrameworkCore.SourceGeneration.ProcedureMapping.Emission;

internal static class ProcedureLoaderEmitter
{
    public static void Emit(
        SourceProductionContext context,
        ModelDiscoveryGeneratorModel loader,
        Compilation compilation,
        ProcedureGenerationContractBindings contracts,
        ImmutableArray<EmittedProcedurePlan> currentPlans)
    {
        List<string> registrations = [];
        HashSet<string> seen = new(StringComparer.Ordinal);

        foreach (EmittedProcedurePlan plan in currentPlans.Where(static plan => plan.IsDiscoverable).OrderBy(static plan => plan.FullyQualifiedName, StringComparer.Ordinal))
        {
            if (seen.Add(plan.FullyQualifiedName))
            {
                registrations.Add($"        {ConstantExpressionFormatter.Qualify(contracts.ProcedureRegistry)}.TryRegister(new global::{plan.FullyQualifiedName}());");
            }
        }

        IEnumerable<INamedTypeSymbol> referencedPlans = DiscoverReferencedPlans(compilation, loader, contracts);
        foreach (INamedTypeSymbol planType in referencedPlans.OrderBy(static type => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), StringComparer.Ordinal))
        {
            string name = ConstantExpressionFormatter.Qualify(planType);
            if (seen.Add(name))
            {
                registrations.Add($"        {ConstantExpressionFormatter.Qualify(contracts.ProcedureRegistry)}.TryRegister(new {name}());");
            }
        }

        string ns = loader.Namespace;
        StringBuilder builder = new();
        builder.AppendLine("#nullable enable");
        builder.AppendLine();
        builder.AppendLine($"namespace {ns};");
        builder.AppendLine();
        builder.AppendLine($"partial class {loader.Class.Name} : {ConstantExpressionFormatter.Qualify(contracts.ProcedurePlanLoader)}");
        builder.AppendLine("{");
        builder.AppendLine($"    void {ConstantExpressionFormatter.Qualify(contracts.ProcedurePlanLoader)}.LoadProcedurePlans()");
        builder.AppendLine("    {");
        if (registrations.Count is 0)
        {
            builder.AppendLine("        // No discoverable generated procedure plans were found.");
        }
        else
        {
            foreach (string registration in registrations)
            {
                builder.AppendLine(registration);
            }
        }

        builder.AppendLine("    }");
        builder.AppendLine("}");

        context.AddSource($"{loader.Class.Name}.ProcedureRegistration.g.cs", SourceText.From(builder.ToString(), Encoding.UTF8));
    }

    public static void EmitModuleInitializer(
        SourceProductionContext context,
        ImmutableArray<EmittedProcedurePlan> plans,
        ProcedureGenerationContractBindings contracts)
    {
        if (plans.IsDefaultOrEmpty)
        {
            return;
        }

        StringBuilder builder = new();
        builder.AppendLine("#nullable enable");
        builder.AppendLine();
        builder.AppendLine("internal static class ProcedurePlanModuleInitializer");
        builder.AppendLine("{");
        builder.AppendLine("    [global::System.Runtime.CompilerServices.ModuleInitializer]");
        builder.AppendLine("    internal static void Initialize()");
        builder.AppendLine("    {");
        foreach (EmittedProcedurePlan plan in plans.OrderBy(static candidate => candidate.FullyQualifiedName, StringComparer.Ordinal))
        {
            builder.AppendLine($"        {ConstantExpressionFormatter.Qualify(contracts.ProcedureRegistry)}.TryRegister(new global::{plan.FullyQualifiedName}());");
        }

        builder.AppendLine("    }");
        builder.AppendLine("}");
        context.AddSource("ProcedurePlanModuleInitializer.g.cs", SourceText.From(builder.ToString(), Encoding.UTF8));
    }

    private static IEnumerable<INamedTypeSymbol> DiscoverReferencedPlans(
        Compilation compilation,
        ModelDiscoveryGeneratorModel loader,
        ProcedureGenerationContractBindings contracts)
    {
        IEnumerable<IAssemblySymbol> assemblies;
        if (loader.Options.TargetAssemblies is { Length: > 0 } targets)
        {
            HashSet<string> names = new(targets, StringComparer.Ordinal);
            assemblies = compilation.GetAllAssemblies().Where(assembly => names.Contains(assembly.Name));
        }
        else
        {
            assemblies = [];
        }

        foreach (IAssemblySymbol assembly in assemblies)
        {
            if (SymbolEqualityComparer.Default.Equals(assembly, compilation.Assembly))
            {
                continue;
            }

            foreach (INamedTypeSymbol type in assembly.GlobalNamespace.GetAllTypes())
            {
                AttributeData? attribute = type.GetAttributes().FirstOrDefault(candidate =>
                    candidate.AttributeClass is { } attributeClass
                    && attributeClass.OriginalDefinition.GetFullMetadataName() == contracts.GeneratedProcedurePlanAttribute.GetFullMetadataName());
                if (attribute is null)
                {
                    continue;
                }

                yield return type;
            }
        }
    }
}
