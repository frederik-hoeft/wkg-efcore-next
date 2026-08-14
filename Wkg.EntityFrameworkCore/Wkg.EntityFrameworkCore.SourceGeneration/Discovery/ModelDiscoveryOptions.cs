using Microsoft.CodeAnalysis;

namespace Wkg.EntityFrameworkCore.SourceGeneration.Discovery;

/// <summary>
/// Generator-side projection of <see cref="ModelLoaderAttribute"/> arguments.
/// </summary>
internal sealed record ModelDiscoveryOptions(
    AssemblyDiscoveryFailureBehavior AssemblyDiscoveryFailureBehavior,
    string[]? TargetAssemblies)
{
    public static ModelDiscoveryOptions FromAttributeData(AttributeData attributeData)
    {
        AssemblyDiscoveryFailureBehavior failureBehavior = AssemblyDiscoveryFailureBehavior.Warning;
        string[]? targetAssemblies = null;

        foreach (KeyValuePair<string, TypedConstant> namedArgument in attributeData.NamedArguments)
        {
            switch (namedArgument.Key)
            {
                case nameof(ModelLoaderAttribute.AssemblyDiscoveryFailureBehavior):
                    if (namedArgument.Value.Value is int enumValue)
                    {
                        failureBehavior = (AssemblyDiscoveryFailureBehavior)enumValue;
                    }
                    break;
                case nameof(ModelLoaderAttribute.TargetAssemblies):
                    if (!namedArgument.Value.Values.IsDefault)
                    {
                        targetAssemblies =
                        [
                            .. namedArgument.Value.Values
                                .Select(static value => value.Value as string)
                                .Where(static value => !string.IsNullOrEmpty(value))
                                .Select(static value => value!)
                        ];
                    }
                    break;
            }
        }

        return new ModelDiscoveryOptions(failureBehavior, targetAssemblies);
    }

    public DiagnosticSeverity GetDiagnosticSeverity() => AssemblyDiscoveryFailureBehavior switch
    {
        AssemblyDiscoveryFailureBehavior.Silent => DiagnosticSeverity.Hidden,
        AssemblyDiscoveryFailureBehavior.Info => DiagnosticSeverity.Info,
        AssemblyDiscoveryFailureBehavior.Warning => DiagnosticSeverity.Warning,
        _ => DiagnosticSeverity.Error
    };
}
