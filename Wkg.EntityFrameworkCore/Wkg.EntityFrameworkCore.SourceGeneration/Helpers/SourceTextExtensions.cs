using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace Wkg.EntityFrameworkCore.SourceGeneration.Helpers;

/// <summary>
/// Loads canonical source units embedded in the analyzer assembly.
/// </summary>
internal static class SourceTextExtensions
{
    extension(SourceText)
    {
        public static SourceText FromEmbedded<T>() => FromEmbedded(typeof(T));

        public static SourceText FromEmbedded(Type type)
        {
            if (type.Assembly != typeof(SourceTextExtensions).Assembly)
            {
                throw new InvalidOperationException($"The canonical type '{type}' must be defined in the source-generator assembly.");
            }

            string typeName = type.FullName
                ?? throw new InvalidOperationException($"The canonical type '{type}' must have a full name.");
            string resourceName = $"{typeName}.cs";
            using Stream stream = typeof(SourceTextExtensions).Assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"The canonical embedded source resource '{resourceName}' was not found.");
            return SourceText.From(stream, Encoding.UTF8, canBeEmbedded: true);
        }
    }
}
