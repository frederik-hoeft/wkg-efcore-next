using Wkg.EntityFrameworkCore.SourceGeneration.Discovery;

namespace Wkg.EntityFrameworkCore.Tests.Model.Discovery;

[ModelLoader(AssemblyDiscoveryFailureBehavior = AssemblyDiscoveryFailureBehavior.Error)]
public sealed partial class TestModelLoader;
