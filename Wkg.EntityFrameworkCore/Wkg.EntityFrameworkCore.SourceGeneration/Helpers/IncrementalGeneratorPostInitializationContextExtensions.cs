using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Wkg.EntityFrameworkCore.SourceGeneration.Helpers;

/// <summary>
/// Bootstraps canonical embedded source units into analyzer-consuming compilations.
/// </summary>
internal static class IncrementalGeneratorPostInitializationContextExtensions
{
    extension(IncrementalGeneratorPostInitializationContext self)
    {
        public void AddCanonicalSource<T>(string hintName) => self.AddCanonicalSource(typeof(T), hintName);

        public void AddCanonicalSource(Type canonicalType, string hintName)
        {
            SourceText sourceText = SourceText.FromEmbedded(canonicalType);
            self.AddSource(hintName, sourceText);
        }
    }
}
