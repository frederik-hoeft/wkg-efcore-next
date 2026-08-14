using Wkg.EntityFrameworkCore.Discovery.SourceGeneration;

namespace Wkg.EntityFrameworkCore.Discovery.SourceGeneration;

/// <summary>
/// Defines the behavior of the source generator when the specified <see cref="ModelLoaderAttribute.TargetAssemblies"/> cannot be found or contain no valid models.
/// </summary>
public enum AssemblyDiscoveryFailureBehavior
{
    /// <summary>
    /// Ignores the failure and continues without loading any models.
    /// </summary>
    Silent,
    /// <summary>
    /// Logs informational message but continues loading other models.
    /// </summary>
    Info,
    /// <summary>
    /// Logs a warning but continues loading other models.
    /// </summary>
    Warning,
    /// <summary>
    /// Emits a compilation error and aborts the build process.
    /// </summary>
    Error
}