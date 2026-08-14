using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;
using Wkg.EntityFrameworkCore.Configuration.Policies;
using Wkg.EntityFrameworkCore.Configuration.Reflection;
using Wkg.EntityFrameworkCore.Configuration.Reflection.Discovery;
using Wkg.Logging;

namespace Wkg.EntityFrameworkCore.Configuration.Discovery;

/// <inheritdoc cref="IEntityDiscoveryContext"/>
/// <summary>
/// Initializes a new instance of the <see cref="ReflectiveEntityDiscoveryContext"/> class using the specified <paramref name="policies"/>.
/// </summary>
/// <param name="policies">The policies to apply to enforce on discovered entities.</param>
public class ReflectiveEntityDiscoveryContext(IEntityPolicy[] policies) : EntityDiscoveryContext(policies), IReflectiveEntityDiscoveryContext
{
    private static readonly ConditionalWeakTable<ModelBuilder, HashSet<Type>?> s_loadedDatabaseEngines = [];
    private readonly Dictionary<Type, IReflectiveModelLoader> _loaders = [];

    void IReflectiveEntityDiscoveryContext.AddLoader(IReflectiveModelLoader loader) => _loaders.Add(loader.GetType(), loader);

    void IReflectiveDiscoveryContext.Discover(ModelBuilder builder, DiscoveryOptions options)
    {
        if (s_loadedDatabaseEngines.TryGetValue(builder, out HashSet<Type>? loadedDatabaseEngines))
        {
            // this ORM model builder has already been configured previously
            // null means that all database engines have been loaded
            _ = loadedDatabaseEngines ?? throw new InvalidOperationException("ORM model builder has already been configured for all reflectively loaded entities.");
            Type[] dbEngineModelAttributeTypes = options.TargetDatabaseEngineAttributes;
            if (options.TargetDatabaseEngineAttributes.Length == 0)
            {
                throw new InvalidOperationException($"Cannot configure ORM model builder for all reflectively loaded entities, since it has already been configured to target specific database engines: {string.Join(", ", loadedDatabaseEngines.Select(t => t.Name))}.");
            }
            foreach (Type type in dbEngineModelAttributeTypes)
            {
                if (!loadedDatabaseEngines.Add(type))
                {
                    throw new InvalidOperationException($"The database engine {type.Name} has already been loaded.");
                }
                Log.WriteInfo($"Added discovery target for entities decorated with {type.Name}.");
            }
        }
        else
        {
            // this ORM model builder has not been configured previously
            loadedDatabaseEngines = options.TargetDatabaseEngineAttributes.Length == 0 ? null : [];
            s_loadedDatabaseEngines.Add(builder, loadedDatabaseEngines);
        }
        foreach (IReflectiveModelLoader loader in _loaders.Values)
        {
            loader.LoadModels(builder, this, options);
        }
    }
}
