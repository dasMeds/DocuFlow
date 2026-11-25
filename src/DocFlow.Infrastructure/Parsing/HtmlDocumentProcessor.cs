using DocFlow.Core.Interfaces;
using DocFlow.Core.Models;
using HtmlAgilityPack;

namespace DocFlow.Infrastructure.Parsing;

/// <summary>
/// Removes executable and presentation-only HTML nodes before extracting decoded text.
/// </summary>
public sealed class HtmlDocumentProcessor : IDocumentProcessor
{
    /// <inheritdoc />
    public IReadOnlyCollection<string> SupportedExtensions { get; } = new[] { ".html", ".htm" };

    /// <inheritdoc />
    public bool CanHandle(string fileExtension)
        => fileExtension.Equals(".html", StringComparison.OrdinalIgnoreCase)
            || fileExtension.Equals(".htm", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public async Task<DocumentTextExtraction> ExtractTextAsync(string filePath, CancellationToken ct = default)
    {
        var source = await File.ReadAllTextAsync(filePath, ct);
        var document = new HtmlDocument();
        document.LoadHtml(source);

        foreach (var node in document.DocumentNode.SelectNodes("//script|//style|//noscript") ?? Enumerable.Empty<HtmlNode>())
            node.Remove();

        var decoded = HtmlEntity.DeEntitize(document.DocumentNode.InnerText);
        var lines = decoded.Split(
            new[] { '\r', '\n', '\t' },
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return new DocumentTextExtraction(string.Join(Environment.NewLine, lines));
    }
}
