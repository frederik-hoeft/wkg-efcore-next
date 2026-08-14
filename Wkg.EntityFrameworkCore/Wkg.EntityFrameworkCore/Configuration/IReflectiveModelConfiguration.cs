namespace Wkg.EntityFrameworkCore.Configuration;

/// <inheritdoc cref="IDiscoverableModelConfiguration{T}"/>
[Obsolete($"{DeprecationNotice.INTERFACE_REMOVAL} Use IDiscoverableModelConfiguration<T> instead.")]
public interface IReflectiveModelConfiguration<T> : IModelConfiguration<T> where T : class, IReflectiveModelConfiguration<T>;