using Microsoft.EntityFrameworkCore;
using Wkg.EntityFrameworkCore.Configuration.Discovery;
using Wkg.EntityFrameworkCore.SourceGeneration.Contracts;
using Wkg.EntityFrameworkCore.SourceGeneration.Discovery.Contracts;

namespace Wkg.EntityFrameworkCore.Configuration;

/// <summary>
/// Defines a contract for loading and configuring entity models into a <see cref="ModelBuilder"/>.
/// </summary>
/// <remarks>
/// Implementations of this interface are responsible for discovering and applying entity model configurations
/// to the Entity Framework Core model builder during the model creation process.
/// </remarks>
[GeneratorContractRegistration<ModelDiscoveryContract>(ModelDiscoveryContract.ModelLoader)]
public interface IModelLoader
{
    /// <summary>
    /// Loads and configures entity models into the specified <see cref="ModelBuilder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="ModelBuilder"/> instance used to configure the entity models.</param>
    /// <param name="discoveryContext">The <see cref="IEntityDiscoveryContext"/> that provides access to discovered entities and their metadata.</param>
    /// <remarks>
    /// This method is called during the model building process to apply entity configurations,
    /// relationships, and other model-specific settings. Implementations should use the provided
    /// discovery context to access information about discovered entities and their configurations.
    /// <para>
    /// Once an entity has been configured and added to the model, it should also be registered
    /// with the <see cref="IEntityDiscoveryContext"/> to ensure it is tracked and available
    /// for further processing. This may be done using the <see cref="EntityDiscoveryHelpers.RegisterInternal{T}(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder{T}, IEntityDiscoveryContext)"/>
    /// method.
    /// </para>
    /// </remarks>
    void LoadModels(ModelBuilder builder, IEntityDiscoveryContext discoveryContext);
}
