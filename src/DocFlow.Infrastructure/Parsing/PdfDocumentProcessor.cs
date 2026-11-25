using DocFlow.Core.Interfaces;
using DocFlow.Core.Models;
using UglyToad.PdfPig;

namespace DocFlow.Infrastructure.Parsing;

/// <summary>
/// Extracts text from each page of a PDF document using PdfPig.
/// </summary>
public sealed class PdfDocumentProcessor : IDocumentProcessor
{
    /// <inheritdoc />
    public IReadOnlyCollection<string> SupportedExtensions { get; } = new[] { ".pdf" };

    /// <inheritdoc />
    public bool CanHandle(string fileExtension)
        => string.Equals(fileExtension, ".pdf", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public Task<DocumentTextExtraction> ExtractTextAsync(string filePath, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        using var document = PdfDocument.Open(filePath);
        var pages = document.GetPages().Select(page => page.Text);
        return Task.FromResult(new DocumentTextExtraction(string.Join(Environment.NewLine, pages)));
    }
}
