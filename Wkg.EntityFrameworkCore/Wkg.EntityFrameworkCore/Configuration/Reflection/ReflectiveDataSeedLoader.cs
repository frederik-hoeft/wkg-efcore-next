using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Collections;
using System.Reflection;
using Wkg.EntityFrameworkCore.Configuration.Discovery;
using Wkg.EntityFrameworkCore.Configuration.Reflection.Discovery;
using Wkg.Logging;
using Wkg.Reflection.Extensions;

namespace Wkg.EntityFrameworkCore.Configuration.Reflection;

/// <summary>
/// Loads and configures all <see cref="IDiscoverableModelDataSeed{T}"/> implementations.
/// </summary>
internal sealed class ReflectiveDataSeedLoader : ReflectiveLoaderBase, IReflectiveModelLoader
{
    /// <summary>
    /// Loads and configures all <see cref="IDiscoverableModelDataSeed{T}"/> implementations.
    /// </summary>
    /// <param name="builder">The <see cref="ModelBuilder"/> to configure.</param>
    /// <param name="discoveryContext">The <see cref="IEntityDiscoveryContext"/> that has been used for model discovery.</param>
    /// <param name="options">The options to use for discovery.</param>
    public void LoadModels(ModelBuilder builder, IEntityDiscoveryContext discoveryContext, DiscoveryOptions options)
    {
        Assembly[]? targetAssemblies = null;
        if (options.TargetAssemblies.Length > 0)
        {
            targetAssemblies = options.TargetAssemblies;
        }
        Type[] dbEngineModelAttributeTypes = options.TargetDatabaseEngineAttributes;

        Log.WriteInfo($"{nameof(ReflectiveDataSeedLoader)} is initializing.");

        ReflectiveDataSeed[] dataSeeds =
        [
            .. TargetAssembliesOrWithEntryPoint(targetAssemblies)
            // get all types in these assemblies
            .SelectMany(asm => asm.GetTypes()
                .Where(type =>
                    // only keep classes
                    type.IsClass
                    // only keep classes that implement IDiscoverableModelDataSeed<T>
                    && type.ImplementsGenericInterfaceDirectly(typeof(IDiscoverableModelDataSeed<>))
                    // only keep classes that have the specified database engine attribute if enabled
                    && (dbEngineModelAttributeTypes.Length == 0 || dbEngineModelAttributeTypes.Any(attribute => type.GetCustomAttribute(attribute) is not null))))
            // just to be sure ...
            .Distinct()
            .Select(type =>
            (
                Type: type,
                TypeArgs: type.GetGenericTypeArgumentsOfSingleDirectInterface(typeof(IDiscoverableModelDataSeed<>))
            ))
            .Where(t => t.TypeArgs is { Length: 1 })
            .Select(type => new ReflectiveDataSeed
            (
                OwnerType: type.Type,
                EntityType: type.TypeArgs![0],
                // get the exact Configure method declared by IModelConfiguration<T>
                GetDataSeed: type.Type.GetMethod
                (
                    nameof(IModelDataSeed<>.GetSeedData),
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly)
                ))
            .Where(dataSeed => dataSeed.GetDataSeed is not null)
        ];
#pragma warning restore CS0618 // Type or member is obsolete

        Log.WriteInfo($"{nameof(ReflectiveDataSeedLoader)} discovered {dataSeeds.Length} model connections.");

        // get the generic Entity method
        MethodInfo? entityTypeBuilderFactory = typeof(ModelBuilder).GetMethod(nameof(ModelBuilder.Entity), 1, []);
        foreach (ReflectiveDataSeed dataSeed in dataSeeds)
        {
            Log.WriteDiagnostic($"{nameof(ReflectiveDataSeedLoader)} loading: {dataSeed.OwnerType.Name}.");
            if (discoveryContext.EntityBuilderCache.TryGetValue(dataSeed.EntityType, out EntityTypeBuilder? entityTypeBuilder) is false)
            {
                // create a new EntityTypeBuilder<T> for the entity type
                // bind the generic method to the entity type
                MethodInfo genericEntityTypeBuilderFactory = entityTypeBuilderFactory!.MakeGenericMethod(dataSeed.EntityType);
                // invoke it to create an EntityTypeBuilder<T> where T matches the entity
                object entityTypeBuilderObj = genericEntityTypeBuilderFactory.Invoke(builder, null)!;
                entityTypeBuilder = (EntityTypeBuilder)entityTypeBuilderObj;
            }
            object[] data = [.. (IEnumerable)dataSeed.GetDataSeed!.Invoke(obj: null, parameters: null)!];
            entityTypeBuilder.HasData(data);

            Log.WriteDiagnostic($"{nameof(ReflectiveDataSeedLoader)} loaded {dataSeed.OwnerType.Name} providing {data.Length} seed data entries for entity type {dataSeed.EntityType.Name}.");
        }
        Log.WriteInfo($"{nameof(ReflectiveDataSeedLoader)} loaded {dataSeeds.Length} model connections.");
        Log.WriteInfo($"{nameof(ReflectiveDataSeedLoader)} is exiting.");
    }
}
