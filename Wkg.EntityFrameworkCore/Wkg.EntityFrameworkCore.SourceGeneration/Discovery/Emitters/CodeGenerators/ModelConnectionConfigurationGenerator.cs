using Microsoft.CodeAnalysis;
using System.Collections.Frozen;
using System.Text;

namespace Wkg.EntityFrameworkCore.SourceGeneration.Discovery.Emitters.CodeGenerators;

/// <summary>
/// Generates a model connection registration for a given model connection.
/// </summary>
internal sealed class ModelConnectionConfigurationGenerator(ModelDiscoveryTypeNames types)
{
    public INamedConfigurationCode GenerateCode(ModelConnection connection) => new ConnectionConfigurationCode(types, connection);

    private sealed class ConnectionConfigurationCode(ModelDiscoveryTypeNames types, ModelConnection connection) : NamedConfigurationCodeBase
    {
        private string? _leftBuilderInstance;
        private string? _rightBuilderInstance;

        public override INamedTypeSymbol Symbol => connection.Connector;

        public override void ResolveDependencies(FrozenDictionary<ITypeSymbol, INamedConfigurationCode> allConfigurations)
        {
            if (allConfigurations.TryGetValue(connection.Left, out INamedConfigurationCode? left))
            {
                _leftBuilderInstance = left.MarkRequired();
            }
            if (allConfigurations.TryGetValue(connection.Right, out INamedConfigurationCode? right))
            {
                _rightBuilderInstance = right.MarkRequired();
            }
        }

        public override IEnumerable<string> EmitSourceLines(ISymbol source, SourceProductionContext context)
        {
            string connectorFullName = Symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            string leftFullName = connection.Left.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            string rightFullName = connection.Right.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            string concreteConnectionType = $"{types.EntityConnectionLoader}<{connectorFullName}, {leftFullName}, {rightFullName}>";
            StringBuilder builder;
            if (string.IsNullOrEmpty(InstanceName))
            {
                builder = new StringBuilder();
            }
            else
            {
                builder = new StringBuilder(new string(' ', 4));
                yield return $"{concreteConnectionType} {InstanceName} =";
            }

            _leftBuilderInstance ??= $"builder.Entity<{leftFullName}>()";
            _rightBuilderInstance ??= $"builder.Entity<{rightFullName}>()";
            builder.Append($"{concreteConnectionType}.Configure(builder, {_leftBuilderInstance}, {_rightBuilderInstance}).Register(context);");
            yield return builder.ToString();
        }
    }
}
