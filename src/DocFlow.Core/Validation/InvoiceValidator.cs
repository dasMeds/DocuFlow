using DocFlow.Core.Models;

namespace DocFlow.Core.Validation;

/// <summary>
/// Enforces domain validation rules before a processed invoice is marked Completed.
/// </summary>
public static class InvoiceValidator
{
    /// <summary>
    /// Validates required invoice fields and checks the subtotal-plus-tax total within one cent.
    /// </summary>
    public static (bool IsValid, string? Issue) Validate(ExtractedInvoice invoice)
    {
        if (string.IsNullOrWhiteSpace(invoice.InvoiceNumber))
            return (false, "Missing required InvoiceNumber.");

        if (string.IsNullOrWhiteSpace(invoice.VendorName))
            return (false, "Missing required VendorName.");

        if (invoice.TotalAmount <= 0m)
            return (false, "TotalAmount must be greater than zero.");

        // Ensure Subtotal + Tax matches Total within a 1-cent tolerance
        var expectedTotal = invoice.Subtotal + invoice.TaxAmount;
        if (Math.Abs(expectedTotal - invoice.TotalAmount) > 0.01m)
            return (false, $"Total mismatch: Subtotal ({invoice.Subtotal:F2}) + Tax ({invoice.TaxAmount:F2}) != Total ({invoice.TotalAmount:F2}).");

        return (true, null);
    }
}
