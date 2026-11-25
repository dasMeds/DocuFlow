using DocFlow.Core.Interfaces;
using DocFlow.Core.Models;

namespace DocFlow.Infrastructure.Parsing;

/// <summary>
/// Reads plain-text and structured text documents without changing their content.
/// </summary>
public sealed class PlainTextDocumentProcessor : IDocumentProcessor
{
    private static readonly HashSet<string> SupportedExtensionSet = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".csv", ".json", ".md"
    };

    /// <inheritdoc />
    public IReadOnlyCollection<string> SupportedExtensions => SupportedExtensionSet;

    /// <inheritdoc />
    public bool CanHandle(string fileExtension) => SupportedExtensionSet.Contains(fileExtension);

    /// <inheritdoc />
    public async Task<DocumentTextExtraction> ExtractTextAsync(string filePath, CancellationToken ct = default)
    {
        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            useAsync: true);
        using var reader = new StreamReader(stream);
        var text = await reader.ReadToEndAsync(ct);
        return new DocumentTextExtraction(text);
    }
}
