using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Runtime.ExceptionServices;
using Wkg.Common.Extensions;
using Wkg.EntityFrameworkCore.Configuration.Policies;
using Wkg.Logging;

namespace Wkg.EntityFrameworkCore.Configuration.Discovery;

/// <summary>
/// A default implementation of <see cref="IEntityDiscoveryContext"/>.
/// </summary>
/// <param name="policies"></param>
public class EntityDiscoveryContext(IEntityPolicy[] policies) : IEntityDiscoveryContext
{
    /// <summary>
    /// Caches the entity builders for discovered entity types.
    /// </summary>
    protected Dictionary<Type, EntityTypeBuilder> EntityBuilderCache { get; } = [];

    IReadOnlyDictionary<Type, EntityTypeBuilder> IEntityDiscoveryContext.EntityBuilderCache => EntityBuilderCache;

    /// <inheritdoc/>
    public IEntityPolicy[] Policies => policies;

    /// <inheritdoc/>
    public virtual void AuditPolicies()
    {
        IEntityDiscoveryContext self = this.To<IEntityDiscoveryContext>();

        // audit for compliance with the specified policies
        Log.WriteInfo($"Auditing {self.EntityBuilderCache.Count} entities for compliance with the specified policies.");
        List<Exception>? exceptions = null;
        foreach (EntityTypeBuilder entityType in self.EntityBuilderCache.Values)
        {
            foreach (IEntityPolicy policy in policies)
            {
                try
                {
                    policy.Audit(entityType.Metadata);
                }
                catch (PolicyViolationException e)
                {
                    Log.WriteException(e, "Policy validation failed.");
                    exceptions ??= [];
                    exceptions.Add(e);
                }
                catch (Exception e)
                {
                    Log.WriteException(e, LogLevel.Fatal);
                    throw;
                }
            }
        }
        if (exceptions is not null)
        {
            AggregateException aggregate = new("One or more policies could not be enforced.", exceptions);
            ExceptionDispatchInfo.SetCurrentStackTrace(aggregate);
            Log.WriteException(aggregate, LogLevel.Fatal);
            throw aggregate;
        }
        Log.WriteInfo($"Audit completed. {self.EntityBuilderCache.Count} entities loaded and configured.");
    }

    void IEntityDiscoveryContext.Register(Type entityType, EntityTypeBuilder builder) => Register(entityType, builder);

    /// <inheritdoc cref="IEntityDiscoveryContext.Register(Type, EntityTypeBuilder)"/>
    protected virtual void Register(Type entityType, EntityTypeBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(entityType);
        ArgumentNullException.ThrowIfNull(builder);

        if (!EntityBuilderCache.TryAdd(entityType, builder))
        {
            throw new InvalidOperationException($"The entity type {entityType.FullName} has already been registered.");
        }
    }
}
