namespace Wkg.EntityFrameworkCore.Discovery.SourceGeneration;

/// <summary>
/// Marks a class for roslyn-based model discovery and source generation. The specified class
/// will be extended to implement the <c>IModelLoader</c> interface and will load all models
/// matching the configuration specified by this attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed partial class ModelLoaderAttribute : Attribute
{
    /// <summary>
    /// Defines the behavior of the source generator when one or more of the specified <see cref="TargetAssemblies"/> cannot be found in the compilation or contain no valid models.
    /// </summary>
    public AssemblyDiscoveryFailureBehavior AssemblyDiscoveryFailureBehavior { get; set; } = AssemblyDiscoveryFailureBehavior.Warning;

    /// <summary>
    /// Gets or sets the names of the target assemblies to search for models. If <see langword="null"/>, all assemblies within the current compilation will be scanned.
    /// </summary>
    public string[]? TargetAssemblies { get; set; }
}