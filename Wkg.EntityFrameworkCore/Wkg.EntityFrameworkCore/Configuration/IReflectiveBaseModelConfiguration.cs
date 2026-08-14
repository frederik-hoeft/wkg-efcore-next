using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Wkg.EntityFrameworkCore.Configuration;

/// <inheritdoc cref="IDiscoverableBaseModelConfiguration{TParentClass}" />
[Obsolete($"{DeprecationNotice.INTERFACE_REMOVAL} Use IDiscoverableBaseModelConfiguration<TParentClass> instead.")]
public interface IReflectiveBaseModelConfiguration<TParentClass> where TParentClass : class, IReflectiveBaseModelConfiguration<TParentClass>
{
    /// <inheritdoc cref="IBaseModelConfiguration{TParentClass}.ConfigureBaseModel{TChildClass}(EntityTypeBuilder{TChildClass})"/>
    internal protected static abstract void ConfigureBaseModel<TChildClass>(EntityTypeBuilder<TChildClass> self)
        where TChildClass : class, TParentClass, IModelConfiguration<TChildClass>;
}