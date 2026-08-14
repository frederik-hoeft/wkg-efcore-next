namespace Wkg.EntityFrameworkCore.SourceGeneration.Discovery;

/// <summary>
/// When applied to a class decorated with <see cref="ModelLoaderAttribute"/>, specifies that only models
/// decorated with the specified attribute <typeparamref name="T"/> should be included in the discovery process.
/// </summary>
/// <typeparam name="T">The attribute type used to filter models during discovery.</typeparam>
/// <remarks>
/// If multiple <see cref="ModelDiscoveryFilterAttribute{T}"/> attributes are applied to the same class,
/// then a union of all specified attributes will be used as filter criteria.
/// </remarks>
[global::System.AttributeUsage(global::System.AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
internal sealed class ModelDiscoveryFilterAttribute<T> : global::System.Attribute where T : global::System.Attribute;
