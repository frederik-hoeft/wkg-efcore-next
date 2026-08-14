using Wkg.EntityFrameworkCore.Configuration.Discovery;
using Wkg.EntityFrameworkCore.Configuration.Policies;

namespace Wkg.EntityFrameworkCore.Configuration;

internal sealed class DiscoverableModelOptionsBuilder : ModelOptionsBuilderBase<IDiscoverableModelOptionsBuilder>, IDiscoverableModelOptionsBuilder
{
    public Func<IEntityPolicy[], IEntityDiscoveryContext>? DiscoveryContextFactory { get; private set; }

    public IDiscoverableModelOptionsBuilder UseDiscoveryContextFactory(Func<IEntityPolicy[], IEntityDiscoveryContext> factory)
    {
        if (DiscoveryContextFactory is not null)
        {
            throw new InvalidOperationException("A discovery context factory has already been configured.");
        }
        DiscoveryContextFactory = factory;
        return Self;
    }
}