using Wkg.EntityFrameworkCore.Configuration.Reflection.Discovery;

namespace Wkg.EntityFrameworkCore.Configuration.Reflection;

internal sealed class ReflectiveModelOptionsBuilder : ModelOptionsBuilderBase<IReflectiveModelOptionsBuilder>, IReflectiveModelOptionsBuilder
{
    private bool _discoveryOptionsConfigured;

    public DiscoveryOptionsBuilder DiscoveryOptionsBuilder { get; } = new();

    public IReflectiveModelOptionsBuilder ConfigureDiscovery(Action<IDiscoveryOptionsBuilder> configure)
    {
        if (_discoveryOptionsConfigured)
        {
            throw new InvalidOperationException("Discovery options have already been configured.");
        }
        configure(DiscoveryOptionsBuilder);
        _discoveryOptionsConfigured = true;
        return Self;
    }
}
