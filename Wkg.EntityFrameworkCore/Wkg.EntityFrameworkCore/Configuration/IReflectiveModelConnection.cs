namespace Wkg.EntityFrameworkCore.Configuration;

/// <inheritdoc cref="IDiscoverableModelConnection{TConnection, TLeft, TRight}"/>
[Obsolete($"{DeprecationNotice.INTERFACE_REMOVAL} Use IDiscoverableModelConnection<TConnection, TLeft, TRight> instead.")]
public interface IReflectiveModelConnection<TConnection, TLeft, TRight> : IModelConnection<TConnection, TLeft, TRight>
    where TConnection : class, IReflectiveModelConnection<TConnection, TLeft, TRight>
    where TLeft : class, IReflectiveModelConfiguration<TLeft>
    where TRight : class, IReflectiveModelConfiguration<TRight>;
