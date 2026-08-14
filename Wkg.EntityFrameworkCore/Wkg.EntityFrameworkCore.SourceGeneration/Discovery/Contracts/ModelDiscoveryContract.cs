namespace Wkg.EntityFrameworkCore.SourceGeneration.Discovery.Contracts;

/// <summary>
/// Stable semantic roles required by the model-discovery source generator.
/// </summary>
internal enum ModelDiscoveryContract
{
    ModelLoader = 1,
    EntityDiscoveryContext = 2,
    ModelConfiguration = 3,
    ModelConnection = 4,
    ModelDataSeed = 5,
    BaseModelConfiguration = 6,
    DiscoverableModelConfiguration = 7,
    DiscoverableModelConnection = 8,
    DiscoverableBaseModelConfiguration = 9,
    DiscoverableModelDataSeed = 10,
    EntityDiscoveryHelpers = 11,
    DatabaseEngineModelAttribute = 12
}
