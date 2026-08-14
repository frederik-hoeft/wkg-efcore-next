namespace Wkg.EntityFrameworkCore.Discovery.SourceGeneration;

/// <summary>
/// Provides version information for the Wkg.EntityFrameworkCore.Discovery.SourceGeneration analyzer.
/// </summary>
internal sealed class WkgEntityFrameworkCoreDiscoverySourceGeneration : DeploymentVersionInfo
{
    private const string CI_DEPLOYMENT__VERSION_PREFIX = "0.0.0";
    private const string CI_DEPLOYMENT__VERSION_SUFFIX = "CI-INJECTED";
    private const string CI_DEPLOYMENT__DATETIME_UTC = "1970-01-01 00:00:00";

    private WkgEntityFrameworkCoreDiscoverySourceGeneration() : base
    (
        CI_DEPLOYMENT__VERSION_PREFIX,
        CI_DEPLOYMENT__VERSION_SUFFIX,
        CI_DEPLOYMENT__DATETIME_UTC
    ) { }

    /// <summary>
    /// Provides version information for the Wkg.EntityFrameworkCore.Discovery.SourceGeneration analyzer.
    /// </summary>
    public static WkgEntityFrameworkCoreDiscoverySourceGeneration VersionInfo { get; } = new WkgEntityFrameworkCoreDiscoverySourceGeneration();
}