using Microsoft.CodeAnalysis;
using Wkg.EntityFrameworkCore.Discovery.SourceGeneration;

namespace Wkg.EntityFrameworkCore.Discovery.SourceGeneration;

// internal part of the ModelLoaderAttribute class, this file is not included in the embedded source added to the compilation
public partial class ModelLoaderAttribute
{
    internal static ModelLoaderAttribute FromAttributeData(AttributeData attributeData)
    {
        ModelLoaderAttribute attribute = new();
        foreach (KeyValuePair<string, TypedConstant> namedArgument in attributeData.NamedArguments)
        {
            switch (namedArgument.Key)
            {
                case nameof(AssemblyDiscoveryFailureBehavior):
                    if (namedArgument.Value.Value is int enumValue)
                    {
                        attribute.AssemblyDiscoveryFailureBehavior = (AssemblyDiscoveryFailureBehavior)enumValue;
                    }
                    break;
                case nameof(TargetAssemblies):
                    if (namedArgument.Value.Values is { } values)
                    {
                        attribute.TargetAssemblies =
                        [
                            .. values
                            .Select(tc => tc.Value?.ToString() ?? string.Empty)
                            .Where(s => !string.IsNullOrEmpty(s))
                        ];
                    }
                    break;
            }
        }
        return attribute;
    }

    internal DiagnosticSeverity GetDiagnosticSeverity() => AssemblyDiscoveryFailureBehavior switch
    {
        AssemblyDiscoveryFailureBehavior.Silent => DiagnosticSeverity.Hidden,
        AssemblyDiscoveryFailureBehavior.Info => DiagnosticSeverity.Info,
        AssemblyDiscoveryFailureBehavior.Warning => DiagnosticSeverity.Warning,
        _ => DiagnosticSeverity.Error
    };
}