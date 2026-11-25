using DocFlow.Core.Interfaces;

namespace DocFlow.Infrastructure.Parsing;

/// <summary>
/// Builds extension-based resolution for the registered document processors.
/// </summary>
public sealed class DocumentProcessorFactory : IDocumentProcessorFactory
{
    private readonly IReadOnlyDictionary<string, IDocumentProcessor> _processors;

    /// <summary>
    /// Builds an extension-to-processor map from the registered processors.
    /// </summary>
    public DocumentProcessorFactory(IEnumerable<IDocumentProcessor> processors)
    {
        _processors = processors
            .SelectMany(processor => processor.SupportedExtensions.Select(extension => (extension, processor)))
            .ToDictionary(
                item => NormalizeExtension(item.extension),
                item => item.processor,
                StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public bool Supports(string fileExtension)
        => _processors.ContainsKey(NormalizeExtension(fileExtension));

    /// <inheritdoc />
    public IDocumentProcessor GetProcessor(string fileExtension)
    {
        var extension = NormalizeExtension(fileExtension);
        return _processors.TryGetValue(extension, out var processor)
            ? processor
            : throw new NotSupportedException($"No document processor is registered for '{extension}'.");
    }

    private static string NormalizeExtension(string extension)
        => extension.StartsWith('.') ? extension : $".{extension}";
}
