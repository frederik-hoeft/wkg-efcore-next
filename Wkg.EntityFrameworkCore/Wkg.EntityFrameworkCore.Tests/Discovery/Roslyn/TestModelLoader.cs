using Wkg.EntityFrameworkCore.Discovery.SourceGeneration;

namespace Wkg.EntityFrameworkCore.Tests.Discovery.Roslyn;

[ModelLoader(AssemblyDiscoveryFailureBehavior = AssemblyDiscoveryFailureBehavior.Error)]
internal sealed partial class TestModelLoader;