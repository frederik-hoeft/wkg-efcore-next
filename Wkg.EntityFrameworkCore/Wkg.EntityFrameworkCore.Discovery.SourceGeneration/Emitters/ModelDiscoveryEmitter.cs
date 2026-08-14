using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Frozen;
using System.Text;
using Wkg.EntityFrameworkCore.Discovery.SourceGeneration;
using Wkg.EntityFrameworkCore.Discovery.SourceGeneration.Discovery;
using Wkg.EntityFrameworkCore.Discovery.SourceGeneration.Emitters.CodeGenerators;
using Wkg.EntityFrameworkCore.Discovery.SourceGeneration.Helpers;

namespace Wkg.EntityFrameworkCore.Discovery.SourceGeneration.Emitters;

/// <summary>
/// Emits the generated IModelLoader implementation for a decorated model loader class.
/// </summary>
internal static class ModelDiscoveryEmitter
{
    // Type mappings used in generated code, since the source generator doesn't hold strong references to EF Core or Wkg.EntityFrameworkCore assemblies
    private static readonly FrozenDictionary<string, string> s_types = new Dictionary<string, string>()
    {
        { "EntityTypeBuilder", "global::Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder" },
        { "ModelBuilder", "global::Microsoft.EntityFrameworkCore.ModelBuilder" },
        { "IModelLoader", "global::Wkg.EntityFrameworkCore.Configuration.IModelLoader" },
        { "IEntityDiscoveryContext", "global::Wkg.EntityFrameworkCore.Configuration.Discovery.IEntityDiscoveryContext" },
        { "IModelConfiguration", "global::Wkg.EntityFrameworkCore.Configuration.IModelConfiguration" },
        { "IModelConnection", "global::Wkg.EntityFrameworkCore.Configuration.IModelConnection" },
        { "IModelDataSeed", "global::Wkg.EntityFrameworkCore.Configuration.IModelDataSeed" },
        { "IBaseModelConfiguration", "global::Wkg.EntityFrameworkCore.Configuration.IBaseModelConfiguration" },
        { "EntityLoader", SymbolNameGenerator.MakeUnique("EntityLoader") },
        { "EntityConnectionLoader", SymbolNameGenerator.MakeUnique("EntityConnectionLoader") },
        { "EntityDataSeedLoader", SymbolNameGenerator.MakeUnique("EntityDataSeedLoader") },
        { "EntityDiscoveryHelpers", "global::Wkg.EntityFrameworkCore.Configuration.Discovery.EntityDiscoveryHelpers" }
    }.ToFrozenDictionary();

    /// <summary>
    /// Emits the generated source code for the model loader.
    /// </summary>
    /// <param name="context">The source production context.</param>
    /// <param name="model">The data model for generation.</param>
    public static void EmitSource(SourceProductionContext context, ModelDiscoveryGeneratorModel model)
    {
        // set up code generators
        ModelConfigurationGenerator modelConfigurationGenerator = new(s_types);
        ModelConnectionConfigurationGenerator modelConnectionConfigurationGenerator = new(s_types);
        ModelDataSeedConfigurationGenerator modelDataSeedConfigurationGenerator = new(s_types);
        CommentGenerator commentGenerator = new(s_types);
        EmptyLineGenerator emptyLineGenerator = new();

        // discover model configurations, connections, and data seeds
        CompilationExplorer explorer = model.CompilationExplorer;
        IEnumerable<INamedConfigurationCode> modelConfigurations = explorer.DiscoverModels(model.Class, context).Select(modelConfigurationGenerator.GenerateCode);
        IEnumerable<INamedConfigurationCode> connectionConfigurations = explorer.DiscoverModelConnections(model.Class, context).Select(modelConnectionConfigurationGenerator.GenerateCode);
        IEnumerable<INamedConfigurationCode> dataSeedConfigurations = explorer.DiscoverDataSeeds(model.Class, context).Select(modelDataSeedConfigurationGenerator.GenerateCode);
        // validate and report discovery results
        explorer.DiscoveryContext.ReportDiscoveryResults(model.Class, context);
        // aggregate all configurations into their desired order
        IConfigurationCode[] allConfigurations = 
        [
            commentGenerator.GenerateCode("load models"),
            ..modelConfigurations,
            commentGenerator.GenerateCode("load model connections"),
            ..connectionConfigurations,
            commentGenerator.GenerateCode("apply data seeds"),
            ..dataSeedConfigurations
        ];
        // resolve interdependencies between configurations (e.g., connections consuming entity builders from the models they connect)
        FrozenDictionary<ITypeSymbol, INamedConfigurationCode> configurationMap = allConfigurations
            .OfType<INamedConfigurationCode>()
            .ToFrozenDictionary<INamedConfigurationCode, ITypeSymbol, INamedConfigurationCode>(static c => c.Symbol, static c => c);
        foreach (IConfigurationCode configuration in allConfigurations)
        {
            configuration.ResolveDependencies(configurationMap);
        }
        // emit source lines for all configurations
        IEnumerable<string> sourceLines = allConfigurations.SelectMany(c => c.EmitSourceLines(model.Class, context));

        // build final source
        StringBuilder sourceBuilder = new(
            $$"""
            #nullable enable

            namespace {{model.Namespace}};

            partial class {{model.Class.Name}} : {{s_types["IModelLoader"]}}
            {
                void {{s_types["IModelLoader"]}}.LoadModels({{s_types["ModelBuilder"]}} builder, {{s_types["IEntityDiscoveryContext"]}} context)
                {
                    global::System.ArgumentNullException.ThrowIfNull(builder);
                    global::System.ArgumentNullException.ThrowIfNull(context);

                    {{string.Join($"\r\n{new string(' ', 2 * 4)}", sourceLines)}}
                }
            }

            file readonly struct {{s_types["EntityConnectionLoader"]}}<TConnection, TLeft, TRight>
                where TConnection : class, {{s_types["IModelConnection"]}}<TConnection, TLeft, TRight>
                where TLeft : class, {{s_types["IModelConfiguration"]}}<TLeft>
                where TRight : class, {{s_types["IModelConfiguration"]}}<TRight>
            {
                internal readonly {{s_types["EntityTypeBuilder"]}}<TConnection> EntityBuilder { get; }

                private {{s_types["EntityConnectionLoader"]}}({{s_types["EntityTypeBuilder"]}}<TConnection> entityBuilder) => EntityBuilder = entityBuilder;

                public static {{s_types["EntityConnectionLoader"]}}<TConnection, TLeft, TRight> Configure({{s_types["ModelBuilder"]}} builder, {{s_types["EntityTypeBuilder"]}}<TLeft> leftBuilder, {{s_types["EntityTypeBuilder"]}}<TRight> rightBuilder)
                {
                    TConnection.Connect(leftBuilder, rightBuilder);
                    return new {{s_types["EntityConnectionLoader"]}}<TConnection, TLeft, TRight>(builder.Entity<TConnection>());
                }

                public {{s_types["EntityTypeBuilder"]}}<TConnection> Register({{s_types["IEntityDiscoveryContext"]}} context)
                {
                    {{s_types["EntityDiscoveryHelpers"]}}.RegisterInternal(EntityBuilder, context);
                    return EntityBuilder;
                }
            }

            file readonly struct {{s_types["EntityLoader"]}}<T> where T : class, {{s_types["IModelConfiguration"]}}<T>
            {
                internal readonly {{s_types["EntityTypeBuilder"]}}<T> EntityBuilder { get; }

                private {{s_types["EntityLoader"]}}({{s_types["EntityTypeBuilder"]}}<T> entityBuilder) =>
                    EntityBuilder = entityBuilder;

                public static {{s_types["EntityLoader"]}}<T> Configure({{s_types["ModelBuilder"]}} builder)
                {
                    {{s_types["EntityTypeBuilder"]}}<T> entityBuilder = builder.Entity<T>();
                    T.Configure(entityBuilder);
                    return new {{s_types["EntityLoader"]}}<T>(entityBuilder);
                }

                public {{s_types["EntityTypeBuilder"]}}<T> Register({{s_types["IEntityDiscoveryContext"]}} context)
                {
                    {{s_types["EntityDiscoveryHelpers"]}}.RegisterInternal(EntityBuilder, context);
                    return EntityBuilder;
                }
            }

            file readonly struct {{s_types["EntityDataSeedLoader"]}}<TEntity, TSeed>
                where TEntity : class
                where TSeed : {{s_types["IModelDataSeed"]}}<TEntity>
            {
                internal readonly {{s_types["EntityTypeBuilder"]}}<TEntity> EntityBuilder { get; }
            
                private {{s_types["EntityDataSeedLoader"]}}({{s_types["EntityTypeBuilder"]}}<TEntity> entityBuilder) =>
                    EntityBuilder = entityBuilder;

                public static {{s_types["EntityDataSeedLoader"]}}<TEntity, TSeed> Configure({{s_types["EntityTypeBuilder"]}}<TEntity> entityBuilder)
                {
                    entityBuilder.HasData(TSeed.GetSeedData());
                    return new {{s_types["EntityDataSeedLoader"]}}<TEntity, TSeed>(entityBuilder);
                }
            }

            file static class {{SymbolNameGenerator.MakeUnique("EntityLoaderExtensions")}}
            {
                public static {{s_types["EntityLoader"]}}<T> ConfigureBase<T, TBase>(this {{s_types["EntityLoader"]}}<T> loader)
                    where TBase : class, {{s_types["IBaseModelConfiguration"]}}<TBase>
                    where T : class, TBase, {{s_types["IModelConfiguration"]}}<T>
                {
                    TBase.ConfigureBaseModel(loader.EntityBuilder);
                    return loader;
                }
            }
            """);
        // add source to context
        SourceText sourceText = SourceText.From(sourceBuilder.ToString(), Encoding.UTF8);
        context.AddSource($"{model.Class.Name}.ModelRegistration.g.cs", sourceText);
    }
}