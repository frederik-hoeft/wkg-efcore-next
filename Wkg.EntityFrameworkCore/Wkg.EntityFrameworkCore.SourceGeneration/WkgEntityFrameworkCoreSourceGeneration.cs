namespace Wkg.EntityFrameworkCore.SourceGeneration;

/// <summary>
/// Provides version information for the Wkg.EntityFrameworkCore.SourceGeneration analyzer package.
/// </summary>
internal sealed class WkgEntityFrameworkCoreSourceGeneration : DeploymentVersionInfo
{
    private const string CI_DEPLOYMENT__VERSION_PREFIX = "0.0.0";
    private const string CI_DEPLOYMENT__VERSION_SUFFIX = "CI-INJECTED";
    private const string CI_DEPLOYMENT__DATETIME_UTC = "1970-01-01 00:00:00";

    private WkgEntityFrameworkCoreSourceGeneration() : base
    (
        CI_DEPLOYMENT__VERSION_PREFIX,
        CI_DEPLOYMENT__VERSION_SUFFIX,
        CI_DEPLOYMENT__DATETIME_UTC
    ) { }

    /// <summary>
    /// Provides version information for the Wkg.EntityFrameworkCore.SourceGeneration analyzer package.
    /// </summary>
    public static WkgEntityFrameworkCoreSourceGeneration VersionInfo { get; } = new();
}
