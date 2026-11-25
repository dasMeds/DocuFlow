using DocFlow.Infrastructure.Parsing;
using Xunit;

namespace DocFlow.Tests;

/// <summary>
/// Confirms OCR setup problems are returned as user-readable diagnostics.
/// </summary>
public sealed class TesseractFallbackTests
{
    [Fact]
    public async Task RecognizeTextAsync_ReturnsDiagnosticWhenLanguageDataIsMissing()
    {
        var tessdata = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var recognizer = new TesseractCliTextRecognizer(tessdata);

        var result = await recognizer.RecognizeTextAsync("unused.png");

        Assert.Empty(result.Text);
        Assert.Contains("eng.traineddata", result.Diagnostic);
    }
}
