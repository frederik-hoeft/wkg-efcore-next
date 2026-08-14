; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID       | Category                  | Severity | Notes
--------------|---------------------------|----------|--------------------
WKGLIBEFC006  | SourceGenerationContracts | Error    | Malformed model discovery source-generation contract registration.
WKGLIBEFC007  | SourceGenerationContracts | Error    | Model discovery source-generation contract is registered more than once.
WKGLIBEFC008  | SourceGenerationContracts | Error    | Required model discovery source-generation contract is missing.
WKGLIBEFC009  | SourceGenerationContracts | Error    | Model discovery source-generation contract has an invalid type shape.
WKGLIBEFC010  | Compatibility             | Error    | The Wkg.EntityFrameworkCore runtime assembly is missing from the compilation.
