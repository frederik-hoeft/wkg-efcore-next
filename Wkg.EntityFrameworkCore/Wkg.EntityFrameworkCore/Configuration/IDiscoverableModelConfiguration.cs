using Microsoft.EntityFrameworkCore;
using Wkg.EntityFrameworkCore.Configuration.Reflection;
using Wkg.EntityFrameworkCore.Extensions;

namespace Wkg.EntityFrameworkCore.Configuration;

/// <summary>
/// Represents a model configuration that will be dynamically configured through a corresponding <see cref="IModelLoader"/> or the <see cref="ModelBuilderExtensions.LoadReflectiveModels(ModelBuilder, Action{IReflectiveModelOptionsBuilder}?)"/> method.
/// </summary>
/// <typeparam name="T">The type of the model.</typeparam>
/// <remarks>
/// <para>
/// This interface must be implemented by the entity to be configured.
/// </para>
/// </remarks>
public interface IDiscoverableModelConfiguration<T> : IModelConfiguration<T> where T : class, IDiscoverableModelConfiguration<T>;