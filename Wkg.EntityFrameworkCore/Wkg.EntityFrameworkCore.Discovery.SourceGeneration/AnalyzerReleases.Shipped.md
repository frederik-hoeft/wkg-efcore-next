; Shipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 10.0

### New Rules

Rule ID       | Category      | Severity | Notes
--------------|---------------|----------|--------------------
WKGLIBEFC001  | Compatibility |  Error   | Incompatible assembly version: Source generator 'Wkg.EntityFrameworkCore.Discovery.SourceGeneration' must have the same version as 'Wkg.EntityFrameworkCore' to ensure compatibility. 'Wkg.EntityFrameworkCore.Discovery.SourceGeneration' has version '{0}', but expected version was '{1}' from 'Wkg.EntityFrameworkCore'.<br>Ensures that the source generator 'Wkg.EntityFrameworkCore.Discovery.SourceGeneration' and the dependent assembly 'Wkg.EntityFrameworkCore' have matching versions to prevent code generation issues due to API mismatches.
WKGLIBEFC002  | Compatibility |  Error   | Missing target assembly for model discovery: Target assembly '{0}' specified in the ModelLoaderAttribute could not be found in the compilation. Ensure that the assembly name is correct and that the assembly is referenced by the project.