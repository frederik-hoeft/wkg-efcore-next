using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace ContractDiscovery.Generator;

/// <summary>
/// Helpers to load embedded source text from this assembly.
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
                throw new InvalidOperationException($"The type {type} must be defined in the same assembly as SourceTextExtensions.");
            }
            string typeName = type.FullName ?? throw new InvalidOperationException("The type must have a full name.");
            // convention-based resource name lookup: file name is the type's full name with .cs extension
            string resourceName = $"{typeName}.cs";
            using Stream stream = typeof(SourceTextExtensions).Assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"The embedded resource '{resourceName}' was not found.");
            return SourceText.From(stream, Encoding.UTF8, canBeEmbedded: true);
        }

        public static SourceText FromEmbedded<T>(string typeName) => FromEmbedded(typeof(T), typeName);

        public static SourceText FromEmbedded(Type type, string typeName)
        {
            string resourceName = $"{type.Namespace}.{typeName}.cs";
            using Stream stream = typeof(SourceTextExtensions).Assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"The embedded resource '{resourceName}' was not found.");
            return SourceText.From(stream, Encoding.UTF8, canBeEmbedded: true);
        }
    }
}