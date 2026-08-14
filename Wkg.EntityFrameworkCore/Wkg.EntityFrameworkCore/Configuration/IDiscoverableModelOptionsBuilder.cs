using Wkg.EntityFrameworkCore.Configuration.Discovery;
using Wkg.EntityFrameworkCore.Configuration.Policies;

namespace Wkg.EntityFrameworkCore.Configuration;

/// <summary>
/// Represents a builder for configuring global model options with discoverable entities.
/// </summary>
public interface IDiscoverableModelOptionsBuilder : IModelOptionsBuilder<IDiscoverableModelOptionsBuilder>
{
    /// <summary>
    /// Configures the factory for creating the entity discovery context.
    /// </summary>
    /// <param name="factory">The factory function that takes an array of <see cref="IEntityPolicy"/> and returns an <see cref="IEntityDiscoveryContext"/>.</param>
    /// <returns>The same <see cref="IDiscoverableModelOptionsBuilder"/> instance for method chaining.</returns>
    IDiscoverableModelOptionsBuilder UseDiscoveryContextFactory(Func<IEntityPolicy[], IEntityDiscoveryContext> factory);
}