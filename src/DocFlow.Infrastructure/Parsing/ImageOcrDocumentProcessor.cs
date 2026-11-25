using DocFlow.Core.Interfaces;
using DocFlow.Core.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace DocFlow.Infrastructure.Parsing;

/// <summary>
/// Normalizes supported image formats to PNG before invoking the configured OCR service.
/// </summary>
public sealed class ImageOcrDocumentProcessor : IDocumentProcessor
{
    private static readonly HashSet<string> SupportedExtensionSet = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".webp"
    };

    private readonly IImageTextRecognizer _recognizer;

    /// <inheritdoc />
    public IReadOnlyCollection<string> SupportedExtensions => SupportedExtensionSet;

    /// <summary>
    /// Creates an image processor with an injectable OCR implementation.
    /// </summary>
    public ImageOcrDocumentProcessor(IImageTextRecognizer recognizer)
    {
        _recognizer = recognizer;
    }

    /// <inheritdoc />
    public bool CanHandle(string fileExtension) => SupportedExtensionSet.Contains(fileExtension);

    /// <inheritdoc />
    public async Task<DocumentTextExtraction> ExtractTextAsync(string filePath, CancellationToken ct = default)
    {
        var normalizedPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.png");
        try
        {
            using var image = await Image.LoadAsync<Rgba32>(filePath, ct);
            await image.SaveAsPngAsync(normalizedPath, ct);
            return await _recognizer.RecognizeTextAsync(normalizedPath, ct);
        }
        finally
        {
            if (File.Exists(normalizedPath))
                File.Delete(normalizedPath);
        }
    }
}

/// <summary>
/// Pluggable OCR boundary for normalized image files.
/// </summary>
public interface IImageTextRecognizer
{
    /// <summary>
    /// Recognizes text from an image and returns a diagnostic rather than throwing for missing setup.
    /// </summary>
    Task<DocumentTextExtraction> RecognizeTextAsync(string imagePath, CancellationToken ct = default);
}
