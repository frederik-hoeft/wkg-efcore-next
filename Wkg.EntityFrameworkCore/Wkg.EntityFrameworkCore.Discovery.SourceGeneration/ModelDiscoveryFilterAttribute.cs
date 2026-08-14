using Wkg.EntityFrameworkCore.Discovery.SourceGeneration;

namespace Wkg.EntityFrameworkCore.Discovery.SourceGeneration;

/// <summary>
/// When applied to a class decorated with <see cref="ModelLoaderAttribute"/>, specifies that only models
/// decorated with the specified attribute <typeparamref name="T"/> should be included in the discovery process.
/// </summary>
/// <typeparam name="T">The attribute type used to filter models during discovery.</typeparam>
/// <remarks>
/// If multiple <see cref="ModelDiscoveryFilterAttribute{T}"/> attributes are applied to the same class,
/// then a union of all specified attributes will be used as filter criteria.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class ModelDiscoveryFilterAttribute<T> : Attribute where T : Attribute;