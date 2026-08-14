using Microsoft.EntityFrameworkCore;
using Wkg.EntityFrameworkCore.Configuration.Reflection;
using Wkg.EntityFrameworkCore.Extensions;

namespace Wkg.EntityFrameworkCore.Configuration;

/// <summary>
/// Represents a many to many connection between two entities that are dynamically configured through a corresponding <see cref="IModelLoader"/> or the <see cref="ModelBuilderExtensions.LoadReflectiveModels(ModelBuilder, Action{IReflectiveModelOptionsBuilder}?)"/> method.
/// </summary>
/// <typeparam name="TConnection">The type of the implementing connection entity.</typeparam>
/// <typeparam name="TLeft">The type of the left entity.</typeparam>
/// <typeparam name="TRight">The type of the right entity.</typeparam>
public interface IDiscoverableModelConnection<TConnection, TLeft, TRight> : IModelConnection<TConnection, TLeft, TRight>
    where TConnection : class, IDiscoverableModelConnection<TConnection, TLeft, TRight>
    where TLeft : class, IDiscoverableModelConfiguration<TLeft>
    where TRight : class, IDiscoverableModelConfiguration<TRight>;