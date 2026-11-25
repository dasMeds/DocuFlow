using DocFlow.Core.Interfaces;
using DocFlow.Core.Models;
using DocFlow.Infrastructure.Parsing;
using Xunit;

namespace DocFlow.Tests;

/// <summary>
/// Verifies extension routing and custom processor registration.
/// </summary>
public sealed class DocumentProcessorFactoryTests
{
    private readonly DocumentProcessorFactory _factory;

    /// <summary>
    /// Registers the built-in parsers and one custom format for factory tests.
    /// </summary>
    public DocumentProcessorFactoryTests()
    {
        _factory = new DocumentProcessorFactory(new IDocumentProcessor[]
        {
            new PlainTextDocumentProcessor(),
            new PdfDocumentProcessor(),
            new OfficeDocumentProcessor(),
            new HtmlDocumentProcessor(),
            new ImageOcrDocumentProcessor(new StubImageTextRecognizer()),
            new CustomDocumentProcessor()
        });
    }

    [Theory]
    [InlineData(".txt", typeof(PlainTextDocumentProcessor))]
    [InlineData(".csv", typeof(PlainTextDocumentProcessor))]
    [InlineData(".json", typeof(PlainTextDocumentProcessor))]
    [InlineData(".md", typeof(PlainTextDocumentProcessor))]
    [InlineData(".PDF", typeof(PdfDocumentProcessor))]
    [InlineData("docx", typeof(OfficeDocumentProcessor))]
    [InlineData(".pptx", typeof(OfficeDocumentProcessor))]
    [InlineData(".html", typeof(HtmlDocumentProcessor))]
    [InlineData(".htm", typeof(HtmlDocumentProcessor))]
    [InlineData(".png", typeof(ImageOcrDocumentProcessor))]
    [InlineData(".jpg", typeof(ImageOcrDocumentProcessor))]
    [InlineData(".jpeg", typeof(ImageOcrDocumentProcessor))]
    [InlineData(".webp", typeof(ImageOcrDocumentProcessor))]
    [InlineData(".invoicex", typeof(CustomDocumentProcessor))]
    public void GetProcessor_UsesExtensionAndSupportsCustomProcessors(string extension, Type expectedType)
    {
        Assert.IsType(expectedType, _factory.GetProcessor(extension));
    }

    [Fact]
    public void GetProcessor_ThrowsForUnsupportedExtension()
    {
        Assert.False(_factory.Supports(".unknown"));
        Assert.Throws<NotSupportedException>(() => _factory.GetProcessor(".unknown"));
    }

    private sealed class CustomDocumentProcessor : IDocumentProcessor
    {
        public IReadOnlyCollection<string> SupportedExtensions { get; } = new[] { ".invoicex" };
        public bool CanHandle(string fileExtension) => SupportedExtensions.Contains(fileExtension);
        public Task<DocumentTextExtraction> ExtractTextAsync(string filePath, CancellationToken ct = default)
            => Task.FromResult(new DocumentTextExtraction(string.Empty));
    }

    private sealed class StubImageTextRecognizer : IImageTextRecognizer
    {
        public Task<DocumentTextExtraction> RecognizeTextAsync(string imagePath, CancellationToken ct = default)
            => Task.FromResult(new DocumentTextExtraction(string.Empty));
    }
}
