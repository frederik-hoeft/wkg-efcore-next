using Microsoft.EntityFrameworkCore;
using Wkg.EntityFrameworkCore.Configuration.Reflection;
using Wkg.EntityFrameworkCore.Extensions;

namespace Wkg.EntityFrameworkCore.Configuration;

/// <summary>
/// Represents a data seed for a model that will be dynamically configured through a corresponding <see cref="IModelLoader"/> or the <see cref="ModelBuilderExtensions.LoadReflectiveModels(ModelBuilder, Action{IReflectiveModelOptionsBuilder}?)"/> method.
/// </summary>
/// <typeparam name="T">The type of the model that the data seed applies to.</typeparam>
public interface IDiscoverableModelDataSeed<T> : IModelDataSeed<T> where T : class;