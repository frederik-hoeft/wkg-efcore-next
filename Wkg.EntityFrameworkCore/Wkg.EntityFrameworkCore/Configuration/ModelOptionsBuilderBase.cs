using System.Diagnostics.CodeAnalysis;
using Wkg.EntityFrameworkCore.Configuration.Policies;
using Wkg.EntityFrameworkCore.Configuration.Policies.Builder;

namespace Wkg.EntityFrameworkCore.Configuration;

internal abstract class ModelOptionsBuilderBase<TSelf> : IModelOptionsBuilder<TSelf> where TSelf : IModelOptionsBuilder<TSelf>
{
    private bool _policyOptionsConfigured;

    protected TSelf Self
    {
        get
        {
            if (this is not TSelf self)
            {
                ThrowInvalidSelfType();
                return default!;
            }
            return self;
        }
    }

    public IPolicyOptionsBuilder PolicyOptionsBuilder { get; } = new PolicyOptionsBuilder();

    public TSelf ConfigurePolicies(Action<IPolicyOptionsBuilder> configure)
    {
        if (_policyOptionsConfigured)
        {
            throw new InvalidOperationException("Policy options have already been configured.");
        }
        configure(PolicyOptionsBuilder);
        _policyOptionsConfigured = true;
        return Self;
    }

    [DoesNotReturn]
    private static void ThrowInvalidSelfType() =>
        throw new InvalidOperationException($"The current instance is not of type '{typeof(TSelf).FullName}'.");
}
