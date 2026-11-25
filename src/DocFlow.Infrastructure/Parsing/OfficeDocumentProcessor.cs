using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Wordprocessing;
using DocFlow.Core.Interfaces;
using DocFlow.Core.Models;
using Drawing = DocumentFormat.OpenXml.Drawing;

namespace DocFlow.Infrastructure.Parsing;

/// <summary>
/// Extracts paragraphs, table text, and slide text from Open XML documents.
/// </summary>
public sealed class OfficeDocumentProcessor : IDocumentProcessor
{
    /// <inheritdoc />
    public IReadOnlyCollection<string> SupportedExtensions { get; } = new[] { ".docx", ".pptx" };

    /// <inheritdoc />
    public bool CanHandle(string fileExtension)
        => fileExtension.Equals(".docx", StringComparison.OrdinalIgnoreCase)
            || fileExtension.Equals(".pptx", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public Task<DocumentTextExtraction> ExtractTextAsync(string filePath, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var extension = Path.GetExtension(filePath);
        var text = extension.Equals(".docx", StringComparison.OrdinalIgnoreCase)
            ? ExtractWordText(filePath)
            : extension.Equals(".pptx", StringComparison.OrdinalIgnoreCase)
                ? ExtractPresentationText(filePath)
                : throw new NotSupportedException($"Office format '{extension}' is not supported.");

        return Task.FromResult(new DocumentTextExtraction(text));
    }

    private static string ExtractWordText(string filePath)
    {
        using var document = WordprocessingDocument.Open(filePath, false);
        var paragraphs = document.MainDocumentPart?.Document?.Body?.Descendants<Paragraph>()
            .Select(paragraph => paragraph.InnerText)
            .Where(text => !string.IsNullOrWhiteSpace(text)) ?? Enumerable.Empty<string>();

        return string.Join(Environment.NewLine, paragraphs);
    }

    private static string ExtractPresentationText(string filePath)
    {
        using var document = PresentationDocument.Open(filePath, false);
        var presentationPart = document.PresentationPart;
        var slideIds = presentationPart?.Presentation?.SlideIdList?.Elements<SlideId>()
            ?? Enumerable.Empty<SlideId>();
        var slideTexts = new List<string>();

        foreach (var slideId in slideIds)
        {
            if (slideId.RelationshipId?.Value is not { } relationshipId
                || presentationPart?.GetPartById(relationshipId) is not SlidePart slidePart)
            {
                continue;
            }

            var textElements = slidePart.Slide?.Descendants<Drawing.Text>();
            if (textElements is null)
                continue;

            var text = string.Join(Environment.NewLine, textElements.Select(item => item.Text));
            if (!string.IsNullOrWhiteSpace(text))
                slideTexts.Add(text);
        }

        return string.Join(Environment.NewLine + Environment.NewLine, slideTexts);
    }
}
