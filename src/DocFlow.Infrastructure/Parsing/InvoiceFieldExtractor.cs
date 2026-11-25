using System.Globalization;
using System.Text.RegularExpressions;
using DocFlow.Core.Interfaces;
using DocFlow.Core.Models;

namespace DocFlow.Infrastructure.Parsing;

/// <summary>
/// Extracts common invoice fields from text produced by any registered document processor.
/// </summary>
public sealed partial class InvoiceFieldExtractor : IInvoiceFieldExtractor
{
    /// <inheritdoc />
    public ExtractedInvoice Extract(string rawText)
    {
        return new ExtractedInvoice
        {
            InvoiceNumber = MatchValue(InvoiceNumberPattern(), rawText)
                ?? MatchValue(FallbackInvoiceNumberPattern(), rawText)
                ?? string.Empty,
            VendorName = MatchValue(VendorPattern(), rawText) ?? string.Empty,
            IssueDate = ParseDate(MatchValue(DatePattern(), rawText)),
            Subtotal = ParseAmount(MatchValue(SubtotalPattern(), rawText)),
            TaxAmount = ParseAmount(MatchValue(TaxPattern(), rawText)),
            TotalAmount = ParseAmount(MatchValue(TotalPattern(), rawText))
        };
    }

    private static string? MatchValue(Regex pattern, string source)
    {
        var match = pattern.Match(source);
        return match.Success ? match.Groups["value"].Value.Trim() : null;
    }

    private static DateOnly? ParseDate(string? value)
        => DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var date)
            ? date
            : null;

    private static decimal ParseAmount(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0m;

        var normalized = string.Concat(value
            .Replace("$", string.Empty, StringComparison.Ordinal)
            .Replace("€", string.Empty, StringComparison.Ordinal)
            .Replace("£", string.Empty, StringComparison.Ordinal)
            .Replace(",", string.Empty, StringComparison.Ordinal)
            .Where(character => !char.IsWhiteSpace(character)));
        return decimal.TryParse(
            normalized,
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowParentheses,
            CultureInfo.InvariantCulture,
            out var amount)
            ? amount
            : 0m;
    }

    [GeneratedRegex(@"(?im)^\s*(?:invoice(?:\s*(?:number|no\.?|#))?|inv)\s*[:#-]?\s*(?<value>[A-Z0-9][A-Z0-9/-]*)\s*$", RegexOptions.CultureInvariant)]
    private static partial Regex InvoiceNumberPattern();

    [GeneratedRegex(@"(?<value>\bINV[-\s]?\d+\b)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex FallbackInvoiceNumberPattern();

    [GeneratedRegex(@"(?im)^\s*(?:vendor(?:\s+name)?|supplier(?:\s+name)?|from|company)\s*[:,-]\s*(?<value>.+?)\s*$", RegexOptions.CultureInvariant)]
    private static partial Regex VendorPattern();

    [GeneratedRegex(@"(?im)^\s*(?:issue\s+date|invoice\s+date|date)\s*[:,-]\s*(?<value>\d{1,4}[/-]\d{1,2}[/-]\d{1,4}|[A-Za-z]+\s+\d{1,2},?\s+\d{4}|\d{1,2}\s+[A-Za-z]+\s+\d{4})\s*$", RegexOptions.CultureInvariant)]
    private static partial Regex DatePattern();

    [GeneratedRegex(@"(?im)^\s*(?:sub\s*total)\s*[:=-]?\s*(?<value>\(?-?\s*(?:[$€£]\s*)?\d[\d,]*(?:\.\d{1,2})?\s*\)?)\s*$", RegexOptions.CultureInvariant)]
    private static partial Regex SubtotalPattern();

    [GeneratedRegex(@"(?im)^\s*(?:tax|vat|gst)\s*[:=-]?\s*(?<value>\(?-?\s*(?:[$€£]\s*)?\d[\d,]*(?:\.\d{1,2})?\s*\)?)\s*$", RegexOptions.CultureInvariant)]
    private static partial Regex TaxPattern();

    [GeneratedRegex(@"(?im)^\s*(?:grand\s+total|total\s+due|total\s+amount|amount\s+due|invoice\s+total|total)\s*[:=-]?\s*(?<value>\(?-?\s*(?:[$€£]\s*)?\d[\d,]*(?:\.\d{1,2})?\s*\)?)\s*$", RegexOptions.CultureInvariant)]
    private static partial Regex TotalPattern();
}
