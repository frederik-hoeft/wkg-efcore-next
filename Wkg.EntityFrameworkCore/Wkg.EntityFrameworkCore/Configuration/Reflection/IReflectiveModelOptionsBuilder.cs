using Wkg.EntityFrameworkCore.Configuration.Reflection.Discovery;

namespace Wkg.EntityFrameworkCore.Configuration.Reflection;

/// <summary>
/// Represents a builder for configuring global model options with reflection-based discovery.
/// </summary>
public interface IReflectiveModelOptionsBuilder : IModelOptionsBuilder<IReflectiveModelOptionsBuilder>
{
    /// <summary>
    /// Configures global entity discovery options.
    /// </summary>
    /// <param name="configure">The action to configure the discovery options.</param>
    /// <returns>The same <see cref="IReflectiveModelOptionsBuilder"/> instance for method chaining.</returns>
    IReflectiveModelOptionsBuilder ConfigureDiscovery(Action<IDiscoveryOptionsBuilder> configure);
}
