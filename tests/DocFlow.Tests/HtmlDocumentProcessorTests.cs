using DocFlow.Infrastructure.Parsing;
using Xunit;

namespace DocFlow.Tests;

/// <summary>
/// Ensures active and presentation-only HTML content is not returned as document text.
/// </summary>
public sealed class HtmlDocumentProcessorTests
{
    [Fact]
    public async Task ExtractTextAsync_RemovesScriptAndStyleContent()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.html");
        await File.WriteAllTextAsync(path, "<html><style>hidden style</style><body>Invoice &amp; tax<script>hidden script</script></body></html>");

        try
        {
            var result = await new HtmlDocumentProcessor().ExtractTextAsync(path);

            Assert.Contains("Invoice & tax", result.Text);
            Assert.DoesNotContain("hidden style", result.Text);
            Assert.DoesNotContain("hidden script", result.Text);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
