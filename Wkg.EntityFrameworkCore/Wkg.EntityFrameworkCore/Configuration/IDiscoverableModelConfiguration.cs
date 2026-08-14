using Microsoft.EntityFrameworkCore;
using Wkg.EntityFrameworkCore.Configuration.Reflection;
using Wkg.EntityFrameworkCore.Extensions;
using Wkg.EntityFrameworkCore.SourceGeneration.Contracts;
using Wkg.EntityFrameworkCore.SourceGeneration.Discovery.Contracts;

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
[GeneratorContractRegistration<ModelDiscoveryContract>(ModelDiscoveryContract.DiscoverableModelConfiguration)]
public interface IDiscoverableModelConfiguration<T> : IModelConfiguration<T> where T : class, IDiscoverableModelConfiguration<T>;
