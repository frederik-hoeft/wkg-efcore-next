using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using Wkg.Logging;
using Wkg.Reflection.Extensions;
using Wkg.EntityFrameworkCore.Configuration.Reflection.Discovery;
using Wkg.EntityFrameworkCore.Configuration.Discovery;

namespace Wkg.EntityFrameworkCore.Configuration.Reflection;

/// <summary>
/// Loads and configures all <see cref="IDiscoverableModelConfiguration{T}"/> implementations.
/// </summary>
internal sealed class ReflectiveModelLoader : ReflectiveLoaderBase, IReflectiveModelLoader
{
    public static readonly string s_runtimeMethodName = $"{typeof(IBaseModelConfiguration<>).Namespace}.{nameof(IBaseModelConfiguration<>)}<{{0}}>.{nameof(IBaseModelConfiguration<>.ConfigureBaseModel)}";
    [Obsolete("This is kept for backward compatibility. will be removed in future major release.")]
    // TODO: remove in future major release
    public static readonly string s_legacyRuntimeMethodName = $"{typeof(IReflectiveBaseModelConfiguration<>).Namespace}.{nameof(IReflectiveBaseModelConfiguration<>)}<{{0}}>.{nameof(IReflectiveBaseModelConfiguration<>.ConfigureBaseModel)}";

    /// <summary>
    /// Loads and configures all <see cref="IDiscoverableModelConfiguration{T}"/> implementations.
    /// </summary>
    /// <param name="builder">The <see cref="ModelBuilder"/> to configure.</param>
    /// <param name="discoveryContext">The <see cref="IEntityDiscoveryContext"/> to use for discovery.</param>
    /// <param name="options">The options to use for discovery.</param>
    public void LoadModels(ModelBuilder builder, IEntityDiscoveryContext discoveryContext, DiscoveryOptions options)
    {
        Assembly[]? targetAssemblies = null;
        if (options.TargetAssemblies.Length > 0)
        {
            targetAssemblies = options.TargetAssemblies;
        }
        Type[] dbEngineModelAttributeTypes = options.TargetDatabaseEngineAttributes;
        Log.WriteInfo($"{nameof(ReflectiveModelLoader)} is initializing.");

#pragma warning disable CS0618 // Type or member is obsolete
        // TODO: drop support for IReflectiveModelConfiguration in future major release
        ReflectiveEntity[] entities = 
        [
            .. TargetAssembliesOrWithEntryPoint(targetAssemblies)
            // get all types in these assemblies
            .SelectMany(asm => asm.GetTypes()
                .Where(type =>
                    // only keep classes
                    type.IsClass
                    // only keep classes that implement IDiscoverableModelConfiguration<T> where T is that exact class
                    && (type.ImplementsDirectGenericInterfaceWithTypeParameter(typeof(IDiscoverableModelConfiguration<>), type)
                        // TODO: drop support for IReflectiveModelConfiguration<T> in future major release
                        || type.ImplementsDirectGenericInterfaceWithTypeParameter(typeof(IReflectiveModelConfiguration<>), type))
                    // only keep classes that have the specified database engine attribute if enabled
                    && (dbEngineModelAttributeTypes.Length == 0 || dbEngineModelAttributeTypes.Any(databaseEngineAttributeType => type.GetCustomAttribute(databaseEngineAttributeType) is not null))))
            // just to be sure...
            .Distinct()
            .Select(type => new ReflectiveEntity
            (
                Type: type,
                // get the exact Configure method declared by IModelConfiguration<T>
                Configure: type.GetMethod
                (
                    nameof(IDiscoverableModelConfiguration<>.Configure),
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly,
                    [typeof(EntityTypeBuilder<>).MakeGenericType(type)])
                // TODO: drop support for IReflectiveModelConfiguration<T> in future major release
                ?? type.GetMethod
                (
                    nameof(IReflectiveModelConfiguration<>.Configure),
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly,
                    [typeof(EntityTypeBuilder<>).MakeGenericType(type)]
                )
            ))
            .Where(entity => entity.Configure is not null)
        ];
#pragma warning restore CS0618 // Type or member is obsolete

        Log.WriteInfo($"{nameof(ReflectiveModelLoader)} discovered {entities.Length} models.");

        // re-use the same array for all calls to Configure
        object[] parameters = new object[1];
        int baseModelsLoaded = 0;
        foreach (ReflectiveEntity entity in entities)
        {
            Log.WriteDiagnostic($"{nameof(ReflectiveModelLoader)} loading: {entity.Type.Name}.");
            // get the generic Entity method
            MethodInfo? entityTypeBuilderFactory = typeof(ModelBuilder).GetMethod(nameof(ModelBuilder.Entity), 1, []);
            // make it generic
            MethodInfo genericEntityTypeBuilderFactory = entityTypeBuilderFactory!.MakeGenericMethod(entity.Type);
            // invoke it to create an EntityTypeBuilder<T> where T matches the entity
            object entityTypeBuilderObj = genericEntityTypeBuilderFactory.Invoke(builder, null)!;
            parameters[0] = entityTypeBuilderObj!;
            // invoke the Configure method with the EntityTypeBuilder<T> instance
            entity.Configure!.Invoke(null, parameters);
            // check if this entity inherits a parent class that implements IDiscoverableBaseModelConfiguration
            Type? baseType = entity.Type.BaseType;
            while (baseType is not null)
            {
                string? methodName = null;
                // recurse up the inheritance tree and look for any base class that implements IDiscoverableBaseModelConfiguration<T> where T is the base class
                if (baseType.ImplementsDirectGenericInterfaceWithTypeParameter(typeof(IDiscoverableBaseModelConfiguration<>), baseType))
                {
                    // load the base model using the explicit interface implementation
                    // we have to do some trickery to get the correct method as it's name is compiler generated.
                    // it would be better to do this using the method table / InterfaceMapping but that just dies with some IL format error.
                    methodName = string.Format(s_runtimeMethodName, baseType.FullName);
                }
#pragma warning disable CS0618 // Type or member is obsolete
                // legacy support for IReflectiveBaseModelConfiguration<T>
                // TODO: to be removed in future major release
                else if (baseType.ImplementsDirectGenericInterfaceWithTypeParameter(typeof(IReflectiveBaseModelConfiguration<>), baseType))
                {
                    methodName = string.Format(s_legacyRuntimeMethodName, baseType.FullName);
                }
#pragma warning restore CS0618 // Type or member is obsolete

                if (methodName is not null)
                {
                    Log.WriteDiagnostic($"{nameof(ReflectiveModelLoader)} found base model: {baseType.Name}.");
                    // we can't filter by arguments as the generic type is not known yet
                    MethodInfo? baseConfigure = baseType.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
                    if (baseConfigure is not null)
                    {
                        // we have the correct method, make it generic to match the child class
                        MethodInfo genericBaseConfigure = baseConfigure.MakeGenericMethod(entity.Type);
                        // invoke it with the EntityTypeBuilder<T> instance where T is the child class.
                        genericBaseConfigure.Invoke(null, parameters);
                        baseModelsLoaded++;
                        Log.WriteDiagnostic($"{nameof(ReflectiveModelLoader)} applied base model definition {baseType.Name} to {entity.Type.Name}.");
                    }
                }
                baseType = baseType.BaseType;
            }
            // enforce policies
            EntityTypeBuilder entityTypeBuilder = (EntityTypeBuilder)entityTypeBuilderObj;
            discoveryContext.Register(entity.Type, entityTypeBuilder);
            Log.WriteDiagnostic($"{nameof(ReflectiveModelLoader)} loaded: {entity.Type.Name}.");
        }
        Log.WriteInfo($"{nameof(ReflectiveModelLoader)} loaded {entities.Length} models and {baseModelsLoaded} base model definitions.");
        Log.WriteInfo($"{nameof(ReflectiveModelLoader)} is exiting.");
    }
}