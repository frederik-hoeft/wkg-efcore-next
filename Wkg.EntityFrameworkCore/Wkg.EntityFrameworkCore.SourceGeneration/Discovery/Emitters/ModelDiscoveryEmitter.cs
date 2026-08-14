using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Frozen;
using System.Text;
using Wkg.EntityFrameworkCore.SourceGeneration.Discovery.Contracts;
using Wkg.EntityFrameworkCore.SourceGeneration.Discovery.Emitters.CodeGenerators;
using Wkg.EntityFrameworkCore.SourceGeneration.Helpers;

namespace Wkg.EntityFrameworkCore.SourceGeneration.Discovery.Emitters;

/// <summary>
/// Emits the generated IModelLoader implementation for a decorated model loader class.
/// </summary>
internal static class ModelDiscoveryEmitter
{
    public static void EmitSource(SourceProductionContext context, ModelDiscoveryGeneratorModel model, Compilation compilation, ModelDiscoveryContractBindings contracts)
    {
        ModelDiscoveryContext discoveryContext = new(model.Options, model.Filters, contracts);
        CompilationExplorer explorer = new(compilation, discoveryContext, contracts);
        ModelDiscoveryTypeNames types = ModelDiscoveryTypeNames.Create(contracts);

        ModelConfigurationGenerator modelConfigurationGenerator = new(types, explorer);
        ModelConnectionConfigurationGenerator modelConnectionConfigurationGenerator = new(types);
        ModelDataSeedConfigurationGenerator modelDataSeedConfigurationGenerator = new(types);
        CommentGenerator commentGenerator = new();

        List<INamedConfigurationCode> modelConfigurations = [.. explorer
            .DiscoverModels(model.Class, context)
            .Select(modelConfigurationGenerator.GenerateCode)];
        List<INamedConfigurationCode> connectionConfigurations = [.. explorer
            .DiscoverModelConnections(model.Class, context)
            .Select(modelConnectionConfigurationGenerator.GenerateCode)];
        List<INamedConfigurationCode> dataSeedConfigurations = [.. explorer
            .DiscoverDataSeeds(model.Class, context)
            .Select(modelDataSeedConfigurationGenerator.GenerateCode)];

        discoveryContext.ReportDiscoveryResults(model.Class, context);

        IConfigurationCode[] allConfigurations =
        [
            commentGenerator.GenerateCode("load models"),
            .. modelConfigurations,
            commentGenerator.GenerateCode("load model connections"),
            .. connectionConfigurations,
            commentGenerator.GenerateCode("apply data seeds"),
            .. dataSeedConfigurations
        ];

        FrozenDictionary<ITypeSymbol, INamedConfigurationCode> configurationMap = allConfigurations
            .OfType<INamedConfigurationCode>()
            .ToFrozenDictionary<INamedConfigurationCode, ITypeSymbol, INamedConfigurationCode>(static configuration => configuration.Symbol, static configuration => configuration);
        foreach (IConfigurationCode configuration in allConfigurations)
        {
            configuration.ResolveDependencies(configurationMap);
        }

        IEnumerable<string> sourceLines = allConfigurations.SelectMany(configuration => configuration.EmitSourceLines(model.Class, context));
        string loaderExtensions = SymbolNameGenerator.MakeUnique("EntityLoaderExtensions");

        StringBuilder sourceBuilder = new(
            $$"""
            #nullable enable

            namespace {{model.Namespace}};

            partial class {{model.Class.Name}} : {{types.ModelLoader}}
            {
                void {{types.ModelLoader}}.LoadModels({{types.ModelBuilder}} builder, {{types.EntityDiscoveryContext}} context)
                {
                    global::System.ArgumentNullException.ThrowIfNull(builder);
                    global::System.ArgumentNullException.ThrowIfNull(context);

                    {{string.Join($"\r\n{new string(' ', 2 * 4)}", sourceLines)}}
                }
            }

            file readonly struct {{types.EntityConnectionLoader}}<TConnection, TLeft, TRight>
                where TConnection : class, {{types.ModelConnection}}<TConnection, TLeft, TRight>
                where TLeft : class, {{types.ModelConfiguration}}<TLeft>
                where TRight : class, {{types.ModelConfiguration}}<TRight>
            {
                internal readonly {{types.EntityTypeBuilder}}<TConnection> EntityBuilder { get; }

                private {{types.EntityConnectionLoader}}({{types.EntityTypeBuilder}}<TConnection> entityBuilder) => EntityBuilder = entityBuilder;

                public static {{types.EntityConnectionLoader}}<TConnection, TLeft, TRight> Configure({{types.ModelBuilder}} builder, {{types.EntityTypeBuilder}}<TLeft> leftBuilder, {{types.EntityTypeBuilder}}<TRight> rightBuilder)
                {
                    TConnection.Connect(leftBuilder, rightBuilder);
                    return new {{types.EntityConnectionLoader}}<TConnection, TLeft, TRight>(builder.Entity<TConnection>());
                }

                public {{types.EntityTypeBuilder}}<TConnection> Register({{types.EntityDiscoveryContext}} context)
                {
                    {{types.EntityDiscoveryHelpers}}.RegisterInternal(EntityBuilder, context);
                    return EntityBuilder;
                }
            }

            file readonly struct {{types.EntityLoader}}<T> where T : class, {{types.ModelConfiguration}}<T>
            {
                internal readonly {{types.EntityTypeBuilder}}<T> EntityBuilder { get; }

                private {{types.EntityLoader}}({{types.EntityTypeBuilder}}<T> entityBuilder) =>
                    EntityBuilder = entityBuilder;

                public static {{types.EntityLoader}}<T> Configure({{types.ModelBuilder}} builder)
                {
                    {{types.EntityTypeBuilder}}<T> entityBuilder = builder.Entity<T>();
                    T.Configure(entityBuilder);
                    return new {{types.EntityLoader}}<T>(entityBuilder);
                }

                public {{types.EntityTypeBuilder}}<T> Register({{types.EntityDiscoveryContext}} context)
                {
                    {{types.EntityDiscoveryHelpers}}.RegisterInternal(EntityBuilder, context);
                    return EntityBuilder;
                }
            }

            file readonly struct {{types.EntityDataSeedLoader}}<TEntity, TSeed>
                where TEntity : class
                where TSeed : {{types.ModelDataSeed}}<TEntity>
            {
                internal readonly {{types.EntityTypeBuilder}}<TEntity> EntityBuilder { get; }

                private {{types.EntityDataSeedLoader}}({{types.EntityTypeBuilder}}<TEntity> entityBuilder) =>
                    EntityBuilder = entityBuilder;

                public static {{types.EntityDataSeedLoader}}<TEntity, TSeed> Configure({{types.EntityTypeBuilder}}<TEntity> entityBuilder)
                {
                    entityBuilder.HasData(TSeed.GetSeedData());
                    return new {{types.EntityDataSeedLoader}}<TEntity, TSeed>(entityBuilder);
                }
            }

            file static class {{loaderExtensions}}
            {
                public static {{types.EntityLoader}}<T> ConfigureBase<T, TBase>(this {{types.EntityLoader}}<T> loader)
                    where TBase : class, {{types.BaseModelConfiguration}}<TBase>
                    where T : class, TBase, {{types.ModelConfiguration}}<T>
                {
                    TBase.ConfigureBaseModel(loader.EntityBuilder);
                    return loader;
                }
            }
            """);

        context.AddSource(
            $"{model.Class.Name}.ModelRegistration.g.cs",
            SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
    }
}
