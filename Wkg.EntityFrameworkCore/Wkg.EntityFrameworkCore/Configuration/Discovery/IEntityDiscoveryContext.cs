using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wkg.EntityFrameworkCore.Configuration.Policies;
using Wkg.EntityFrameworkCore.SourceGeneration.Contracts;
using Wkg.EntityFrameworkCore.SourceGeneration.Discovery.Contracts;

namespace Wkg.EntityFrameworkCore.Configuration.Discovery;

/// <summary>
/// A context for entity discovery that can be used to enforce naming and mapping policies on discovered entities.
/// </summary>
[GeneratorContractRegistration<ModelDiscoveryContract>(ModelDiscoveryContract.EntityDiscoveryContext)]
public interface IEntityDiscoveryContext
{
    internal IReadOnlyDictionary<Type, EntityTypeBuilder> EntityBuilderCache { get; }

    /// <summary>
    /// The policies to enforce on discovered entities.
    /// </summary>
    IEntityPolicy[] Policies { get; }

    /// <summary>
    /// Audits all discovered entities for compliance with the policies and takes corresponding actions if necessary.
    /// </summary>
    void AuditPolicies();

    /// <summary>
    /// Registers a discovered entity type along with its builder.
    /// </summary>
    /// <param name="entityType">The entity type.</param>
    /// <param name="builder">The entity type builder.</param>
    internal protected void Register(Type entityType, EntityTypeBuilder builder);
}
