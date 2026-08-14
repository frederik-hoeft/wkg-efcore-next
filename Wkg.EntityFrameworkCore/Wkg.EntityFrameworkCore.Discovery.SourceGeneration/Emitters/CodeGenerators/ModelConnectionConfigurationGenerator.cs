using Microsoft.CodeAnalysis;
using System.Collections.Frozen;
using System.Text;
using Wkg.EntityFrameworkCore.Discovery.SourceGeneration;

namespace Wkg.EntityFrameworkCore.Discovery.SourceGeneration.Emitters.CodeGenerators;

/// <summary>
/// Generates a model connection registration for a given model connection.
/// </summary>
/// <param name="types">The frozen dictionary of type name mappings.</param>
internal sealed class ModelConnectionConfigurationGenerator(FrozenDictionary<string, string> types)
{
    private readonly FrozenDictionary<string, string> _types = types;

    public INamedConfigurationCode GenerateCode(ModelConnection connection) => new ConnectionConfigurationCode(this, connection);

    private sealed class ConnectionConfigurationCode(ModelConnectionConfigurationGenerator generator, ModelConnection connection) : NamedConfigurationCodeBase
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
            StringBuilder builder;
            string concreteConnectionType = $"{generator._types["EntityConnectionLoader"]}<{connectorFullName}, {leftFullName}, {rightFullName}>";
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
            builder.Append(concreteConnectionType).Append(".Configure(builder, ").Append(_leftBuilderInstance).Append(", ").Append(_rightBuilderInstance).Append(").Register(context);");
            yield return builder.ToString();
        }
    }
}
