#nullable enable

namespace Wkg.EntityFrameworkCore.SourceGeneration.Discovery;

/// <summary>
/// Marks a class for Roslyn-based model discovery and source generation. The specified class
/// will be extended to implement the <c>IModelLoader</c> interface and will load all models
/// matching the configuration specified by this attribute.
/// </summary>
[global::System.AttributeUsage(global::System.AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
internal sealed class ModelLoaderAttribute : global::System.Attribute
{
    /// <summary>
    /// Defines the behavior of the source generator when one or more of the specified <see cref="TargetAssemblies"/> cannot be found in the compilation or contain no valid models.
    /// </summary>
    public AssemblyDiscoveryFailureBehavior AssemblyDiscoveryFailureBehavior { get; set; } = AssemblyDiscoveryFailureBehavior.Warning;

    /// <summary>
    /// Gets or sets the names of the target assemblies to search for models. If <see langword="null"/>, only the current compilation assembly is scanned.
    /// </summary>
    public string[]? TargetAssemblies { get; set; }
}
