; Shipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 10.0

### New Rules

Rule ID       | Category      | Severity | Notes
--------------|---------------|----------|--------------------
WKGLIBEFC001  | Compatibility |  Error   | Incompatible assembly version: Source generator 'Wkg.EntityFrameworkCore.SourceGeneration' must have the same version as 'Wkg.EntityFrameworkCore' to ensure compatibility. 'Wkg.EntityFrameworkCore.SourceGeneration' has version '{0}', but expected version was '{1}' from 'Wkg.EntityFrameworkCore'.<br>Ensures that the source generator 'Wkg.EntityFrameworkCore.SourceGeneration' and the dependent assembly 'Wkg.EntityFrameworkCore' have matching versions to prevent code generation issues due to API mismatches.
WKGLIBEFC002  | ModelDiscovery |  Error   | Missing target assembly for model discovery: Target assembly '{0}' specified in the ModelLoaderAttribute could not be found in the compilation. Ensure that the assembly name is correct and that the assembly is referenced by the project.
WKGLIBEFC003  | Usage         |  Error   | Invalid model discovery filter attribute: The type argument '{0}' of the model discovery filter attribute must derive from DatabaseEngineModelAttribute.
WKGLIBEFC004  | Design        |  Warning | No discoverable models found: No discoverable models implementing IDiscoverableModelConfiguration<T> were found in the specified assemblies.
WKGLIBEFC005  | Design        |  Warning | No discoverable models found in assembly: Assembly '{0}' does not contain any discoverable models implementing IDiscoverableModelConfiguration<T>.
WKGLIBEFC006  | SourceGenerationContracts | Error    | Malformed model discovery source-generation contract registration.
WKGLIBEFC007  | SourceGenerationContracts | Error    | Model discovery source-generation contract is registered more than once.
WKGLIBEFC008  | SourceGenerationContracts | Error    | Required model discovery source-generation contract is missing.
WKGLIBEFC009  | SourceGenerationContracts | Error    | Model discovery source-generation contract has an invalid type shape.
WKGLIBEFC010  | Compatibility             | Error    | The Wkg.EntityFrameworkCore runtime assembly is missing from the compilation.
