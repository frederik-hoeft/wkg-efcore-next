using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wkg.EntityFrameworkCore.SourceGeneration.Contracts;
using Wkg.EntityFrameworkCore.SourceGeneration.Discovery.Contracts;

namespace Wkg.EntityFrameworkCore.Configuration.Discovery;

/// <summary>
/// Provides helper methods for entity discovery and registration operations.
/// </summary>
/// <remarks>
/// This class contains utility methods that facilitate the registration of entities
/// during the model discovery process. These methods are typically used internally
/// by model loaders and configuration systems and should not be called directly in application code.
/// </remarks>
[GeneratorContractRegistration<ModelDiscoveryContract>(ModelDiscoveryContract.EntityDiscoveryHelpers)]
public static class EntityDiscoveryHelpers
{
    /// <summary>
    /// Registers an entity type with the discovery context using an unsafe registration method.
    /// </summary>
    /// <typeparam name="T">The entity type to register. Must be a reference type.</typeparam>
    /// <param name="entityTypeBuilder">The <see cref="EntityTypeBuilder{TEntity}"/> instance for the entity type.</param>
    /// <param name="context">The <see cref="IEntityDiscoveryContext"/> to register the entity with.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="entityTypeBuilder"/> or <paramref name="context"/> is <see langword="null"/>.
    /// </exception>
    public static void RegisterInternal<T>(EntityTypeBuilder<T> entityTypeBuilder, IEntityDiscoveryContext context) where T : class
    {
        ArgumentNullException.ThrowIfNull(entityTypeBuilder);
        ArgumentNullException.ThrowIfNull(context);
        context.Register(typeof(T), entityTypeBuilder);
    }
}
