using Wkg.EntityFrameworkCore.Configuration.Policies;

namespace Wkg.EntityFrameworkCore.Configuration;

/// <summary>
/// Represents a builder for configuring global model options.
/// </summary>
public interface IModelOptionsBuilder<TSelf> where TSelf : IModelOptionsBuilder<TSelf>
{
    /// <summary>
    /// Configures global entity validation policies.
    /// </summary>
    /// <param name="configure">The action to configure the policy options.</param>
    /// <returns>The same <see cref="IModelOptionsBuilder{TSelf}"/> instance for method chaining.</returns>
    TSelf ConfigurePolicies(Action<IPolicyOptionsBuilder> configure);
}